using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai;

internal readonly record struct SamuraiDebugState
{
    public long Now { get; init; }
    public ulong PlayerId { get; init; }
    public bool InCombat { get; init; }
    public bool Alive { get; init; }
    public bool HasTarget { get; init; }
    public float Gcd { get; init; }
    public float GcdLeft { get; init; }
    public string Context { get; init; }
    public string Qts { get; init; }
    public string Transition { get; init; }
    public string Details { get; init; }
}

// 回调只入队，游戏线程整理状态，后台顺序写文件。
internal sealed class SamuraiDebugLog
{
    private readonly record struct Signal(string Kind, ulong Source, ulong Target, uint Id, uint Sequence, long At, string Detail);
    private readonly record struct Selection(long Number, uint Id, uint Adjusted, long At);
    private readonly Func<string> _directory;
    private readonly ConcurrentQueue<Signal> _signals = new();
    private readonly Queue<(long At, DateTime Wall, string Text)> _prepull = new();
    private readonly Queue<(uint Id, uint Sequence)> _seen = new();
    private readonly List<Selection> _selections = new();
    private readonly Channel<(string Path, string? Line)> _writes = Channel.CreateBounded<(string, string?)>(512);
    private Task? _writer;
    private volatile bool _enabled;
    private volatile string _error = "";
    private ulong _playerId;
    private SamuraiDebugState _state;
    private string? _path;
    private bool _combat, _closed, _partial, _stalled;
    private int _signalCount, _effects;
    private long _pullAt, _lastGcdAt, _selectionNumber;
    private long _lastGcdSelectionAt, _lastOffSelectionAt;
    private string _gcdSelection = "", _offSelection = "", _qts = "", _transition = "";

    private static readonly Dictionary<uint, string> Names = typeof(SAMSkill).GetFields()
        .ToDictionary(f => (uint)f.GetValue(null)!, f => f.Name.Replace('_', '·'));
    private static readonly HashSet<uint> Gcds = new()
    {
        SAMSkill.刃风, SAMSkill.晓风, SAMSkill.阵风, SAMSkill.士风, SAMSkill.雪风,
        SAMSkill.月光, SAMSkill.花车, SAMSkill.风雅, SAMSkill.风光, SAMSkill.满月, SAMSkill.樱花,
        SAMSkill.燕飞, SAMSkill.居合术, SAMSkill.彼岸花, SAMSkill.天下五剑, SAMSkill.纷乱雪月花,
        SAMSkill.天道五剑, SAMSkill.天道雪月花, SAMSkill.燕回返, SAMSkill.回返五剑,
        SAMSkill.回返雪月花, SAMSkill.天道回返五剑, SAMSkill.天道回返雪月花, SAMSkill.奥义斩浪, SAMSkill.回返斩浪
    };

    internal SamuraiDebugLog(Func<string> directory) => _directory = directory;
    public bool Enabled => _enabled;
    public string Error => _error;
    public string FilePath => _path ?? "";
    internal Task Completion => _writer ?? Task.CompletedTask;
    internal static string ActionName(uint id) => Names.GetValueOrDefault(id, id.ToString());

    public void Enable(SamuraiDebugState state)
    {
        if (_closed || _error.Length != 0 || _enabled) return;
        _state = state; Volatile.Write(ref _playerId, state.PlayerId);
        _enabled = true;
        _qts = _transition = _gcdSelection = _offSelection = "";
        if (state.InCombat && !_combat) CombatStarted(state.Now, true);
        else if (_combat) { _partial = true; OpenRound(state.Now, "中途开启/继续记录，非完整战斗"); }
        Frame(state);
    }

    public void Disable(long now)
    {
        DrainSignals();
        if (_enabled) Record(now, "记录停止；同场再次开启会追加并标记缺口");
        _enabled = false;
        if (_combat) _partial = true;
        CloseFile();
        ClearPending();
    }

    public void CombatStarted(long now, bool partial = false)
    {
        if (_combat) return;
        _combat = true; _pullAt = _lastGcdAt = now;
        _effects = 0; _selectionNumber = 0; _path = null; _stalled = false;
        _partial = partial || !_enabled;
        if (_enabled) OpenRound(now, partial ? "中途开始记录，非完整战斗" : "战斗开始");
    }

    public void CombatEnded(long now, string reason)
    {
        DrainSignals();
        if (_combat && _enabled)
            Record(now, $"战斗结束：{reason}，记录={(_partial ? "部分" : "完整")} 自身技能效果={_effects} 自动选招={_selectionNumber}");
        CloseFile();
        _combat = false; _path = null; _pullAt = 0;
        _qts = _transition = "";
        ClearPending();
    }

    public void Shutdown(long now)
    {
        if (_closed) return;
        CombatEnded(now, "切换职业/卸载ACR");
        _enabled = false; _closed = true;
        _writes.Writer.TryComplete();
    }

    private void ClearPending()
    {
        while (_signals.TryDequeue(out _)) Interlocked.Decrement(ref _signalCount);
        _prepull.Clear(); _seen.Clear(); _selections.Clear();
        _gcdSelection = _offSelection = "";
        _lastGcdSelectionAt = _lastOffSelectionAt = 0; _stalled = false;
    }

    public void ObserveEffect(ulong source, ulong target, uint id, uint sequence, long now) =>
        Enqueue(new("效果", source, target, id, sequence, now, ""));

    public void ObserveCast(ulong source, ulong target, uint id, long now, string kind = "开始咏唱") =>
        Enqueue(new(kind, source, target, id, 0, now, ""));

    public void ObserveHiganbana(ulong source, ulong target, uint status, float duration, string kind, long now)
    {
        if (status == SAMBuff.彼岸花)
            Enqueue(new("彼岸花状态" + kind, source, target, status, 0, now, $"剩余={duration:F3}s"));
    }

    public void ObserveNote(long now, string text) =>
        Enqueue(new("事件", Volatile.Read(ref _playerId), 0, 0, 0, now, text));

    private void Enqueue(Signal signal)
    {
        if (!_enabled || signal.Source != Volatile.Read(ref _playerId)) return;
        if (signal.Kind == "效果" && signal.Id <= 8) return;
        if (Interlocked.Increment(ref _signalCount) > 256)
        {
            Interlocked.Decrement(ref _signalCount);
            Fail(new IOException("事件缓冲已满，已停止记录"));
            return;
        }
        _signals.Enqueue(signal);
    }

    public void Select(uint id, uint adjusted, ulong target, bool off, SamuraiDebugState state, string reason)
    {
        if (!_enabled) return;
        _state = state;
        var key = $"{id}/{adjusted}/{target:X} {reason}";
        var old = off ? _offSelection : _gcdSelection;
        var at = off ? _lastOffSelectionAt : _lastGcdSelectionAt;
        // 同一候选反复查询不刷屏；空结果只记原因变化。
        if (key == old && (id == 0 || state.Now - at < 1000)) return;
        if (off) { _offSelection = key; _lastOffSelectionAt = state.Now; }
        else { _gcdSelection = key; _lastGcdSelectionAt = state.Now; }
        if (id == 0)
        {
            Record(state.Now, $"{(off ? "能力技" : "GCD")}等待 原因={reason}｜观察 {state.Details}");
            return;
        }
        if (_selections.Count >= 64) _selections.RemoveAt(0);
        _selections.Add(new(++_selectionNumber, id, adjusted, state.Now));
        Record(state.Now, $"选招#{_selectionNumber} {(off ? "能力技" : "GCD")}={ActionName(adjusted)} 基础ID={id} 调整ID={adjusted} 目标={target:X} 原因={reason}｜选招前 {state.Details}");
    }

    public void Frame(SamuraiDebugState state)
    {
        _state = state; Volatile.Write(ref _playerId, state.PlayerId);
        if (!_enabled) return;
        if (state.InCombat && !_combat) CombatStarted(state.Now);
        if (!state.InCombat && _combat) { CombatEnded(state.Now, "脱战"); return; }
        DrainSignals();
        if (state.Qts != _qts) { _qts = state.Qts; Record(state.Now, "QT/设置 " + _qts); }
        if (state.Transition != _transition)
        {
            _transition = state.Transition;
            Record(state.Now, $"状态 {_transition}｜观察 {state.Details}");
        }
        foreach (var selection in _selections.Where(s => state.Now - s.At > 5000))
            Record(state.Now, $"选招#{selection.Number} 尚未观察到匹配效果；不代表已发出或失败");
        _selections.RemoveAll(s => state.Now - s.At > 5000);
        // 能力技效果不能掩盖公共技能停手。
        var stalled = state.Alive && state.InCombat && state.HasTarget && state.GcdLeft <= .3f &&
            state.Now - _lastGcdAt > Math.Max(3, state.Gcd + .75f) * 1000;
        if (stalled && !_stalled) Record(state.Now, $"公共技能未见新效果 最近选招={_gcdSelection}｜观察 {state.Details}");
        if (!stalled && _stalled) Record(state.Now, "公共技能停手观察已解除");
        _stalled = stalled;
    }

    private void DrainSignals()
    {
        while (_signals.TryDequeue(out var e))
        {
            Interlocked.Decrement(ref _signalCount);
            if (!_enabled || e.Source != _playerId) continue;
            if (e.Kind == "事件") { Record(e.At, e.Detail); continue; }
            if (e.Kind != "效果")
            {
                Record(e.At, $"{e.Kind} ID={e.Id} 来源={e.Source:X} 目标={e.Target:X} {e.Detail}");
                continue;
            }
            if (_seen.Contains((e.Id, e.Sequence))) continue;
            _seen.Enqueue((e.Id, e.Sequence));
            if (_seen.Count > 64) _seen.Dequeue();
            _effects++;
            if (Gcds.Contains(e.Id)) { _lastGcdAt = e.At; _gcdSelection = ""; }
            else _offSelection = "";
            var match = _selections.FindLastIndex(s => (s.Id == e.Id || s.Adjusted == e.Id) && e.At >= s.At && e.At - s.At <= 5000);
            var selection = match >= 0 ? $"对应候选#{_selections[match].Number}" : "未匹配自动选招";
            // 同一动作可能有多次候选查询，成功后一起结束等待。
            if (match >= 0) _selections.RemoveAll(s => (s.Id == e.Id || s.Adjusted == e.Id) && s.At <= e.At);
            Record(e.At, $"效果 {ActionName(e.Id)} ID={e.Id} seq={e.Sequence} 来源={e.Source:X} 首个效果目标={e.Target:X} {selection}｜框架观察@{(_state.Now - _pullAt) / 1000f:F3}s {_state.Details}");
        }
    }

    private void OpenRound(long now, string reason)
    {
        try
        {
            _path ??= Path.Combine(_directory(), $"SAM_{DateTime.Now:yyyyMMdd-HHmmss-fff}_{Guid.NewGuid().ToString("N")[..6]}.log");
            _writer ??= Task.Run(WriteFiles);
            Record(now, $"{reason}｜{_state.Context}｜player={_state.PlayerId:X}｜{_state.Details}");
            Record(now, "QT/设置初始 " + _state.Qts);
            while (_prepull.TryDequeue(out var line))
                if (now - line.At <= 60000) Write(line.At, line.Wall, line.Text);
        }
        catch (Exception ex) { Fail(ex); }
    }

    private void Record(long now, string text)
    {
        if (!_enabled) return;
        text = text.Replace('\r', ' ').Replace('\n', ' ');
        if (_combat && _path != null) Write(now, DateTime.Now, text);
        else
        {
            while (_prepull.TryPeek(out var old) && (now - old.At > 60000 || _prepull.Count >= 96)) _prepull.Dequeue();
            _prepull.Enqueue((now, DateTime.Now, text));
        }
    }

    private void Write(long now, DateTime wall, string text)
    {
        var line = string.Create(CultureInfo.InvariantCulture, $"{wall:HH:mm:ss.fff} {(now - _pullAt) / 1000d:+0.000;-0.000;0.000}s [SAM] {text}");
        if (!_writes.Writer.TryWrite((_path!, line))) Fail(new IOException("写入缓冲已满，已停止记录"));
    }

    private void CloseFile()
    {
        if (_path != null && !_writes.Writer.TryWrite((_path, null))) Fail(new IOException("关闭缓冲已满"));
    }

    private async Task WriteFiles()
    {
        StreamWriter? file = null;
        string? path = null;
        try
        {
            while (await _writes.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (_writes.Reader.TryRead(out var item))
                {
                    if (item.Line == null)
                    {
                        if (file != null) await file.DisposeAsync().ConfigureAwait(false);
                        file = null; path = null; continue;
                    }
                    if (file == null || path != item.Path)
                    {
                        if (file != null) await file.DisposeAsync().ConfigureAwait(false);
                        Directory.CreateDirectory(Path.GetDirectoryName(item.Path)!);
                        file = new StreamWriter(new FileStream(item.Path, FileMode.Append, FileAccess.Write, FileShare.Read,
                            16384, FileOptions.Asynchronous), new UTF8Encoding(false));
                        path = item.Path;
                    }
                    await file.WriteLineAsync(item.Line).ConfigureAwait(false);
                }
                if (file != null) await file.FlushAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex) { Fail(ex); }
        finally
        {
            try { if (file != null) await file.DisposeAsync().ConfigureAwait(false); }
            catch (Exception ex) { Fail(ex); }
        }
    }

    internal void Fail(Exception ex)
    {
        _error = $"SAM记录失败：{ex.GetType().Name} {ex.Message}";
        _enabled = false;
        _writes.Writer.TryComplete();
    }
}
