using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper;

// 游戏线程只整理有意义的变化；日志回调只入队，文件由一个后台写入者顺序处理。
internal sealed class ReaperDebugLog
{
    private readonly Func<string> _directory;
    private readonly ConcurrentQueue<(bool Cast, ulong Source, uint Id, uint Sequence, long At)> _signals = new();
    private readonly Queue<(long At, DateTime Wall, string Text)> _prepull = new();
    private readonly Queue<(uint Id, uint Sequence)> _seen = new();
    private readonly List<(long Number, uint Id, long At)> _requests = new();
    private readonly Channel<(string Path, string? Line)> _writes = Channel.CreateBounded<(string, string?)>(512);
    private Task? _writer;
    private volatile bool _enabled;
    private volatile string _error = "";
    private ulong _playerId;
    private string? _path;
    private bool _combat, _closed, _stalled, _partial;
    private long _pullAt, _lastEffectAt, _requestNumber, _lastRequestAt;
    private uint _lastRequestId, _castId;
    private int _signalCount, _dropped, _effects, _windowVersion = -1;
    private int _communios, _perfectios, _gluttonies, _circles, _buffCommunios, _buffPerfectios;
    private string _qts = "", _transition = "", _decision = "";
    private string _context = "", _window = "";
    private ReaperState _state;

    internal ReaperDebugLog(Func<string> directory) => _directory = directory;
    public bool Enabled => _enabled;
    public string Error => _error;
    public string FilePath => _path ?? "";
    internal Task Completion => _writer ?? Task.CompletedTask;

    public void Enable(ReaperState state, string context)
    {
        if (_closed || _error.Length != 0) return;
        _state = state; _playerId = state.PlayerId; _context = context;
        _enabled = true;
        _qts = _transition = _decision = ""; _windowVersion = -1;
        if (state.InCombat && !_combat) CombatStarted(state.Now, true);
        else if (_combat) { _partial = true; OpenRound(state.Now, "中途开启/继续记录，非完整战斗"); }
    }

    public void Disable(long now)
    {
        if (_enabled) Record(now, "记录停止（战斗若未结束，再开启将追加本文件）");
        _enabled = false;
        if (_combat) _partial = true;
        CloseFile();
        ClearPending();
    }

    public void CombatStarted(long now, bool partial = false)
    {
        if (_combat) return;
        _combat = true; _pullAt = _lastEffectAt = now;
        _effects = 0; _requestNumber = 0; _path = null; _stalled = false;
        _partial = partial || !_enabled;
        _communios = _perfectios = _gluttonies = _circles = _buffCommunios = _buffPerfectios = 0;
        if (_enabled) OpenRound(now, partial ? "中途开始记录，非完整战斗" : "战斗开始");
    }

    public void CombatEnded(long now, string reason)
    {
        DrainSignals();
        if (_combat && _enabled) Record(now, $"战斗结束：{reason}，记录={(_partial ? "部分" : "完整")} 自身技能效果={_effects} 请求={_requestNumber} 神秘环={_circles} 暴食={_gluttonies} 团契={_communios}/团辅观察{_buffCommunios} 完人={_perfectios}/团辅观察{_buffPerfectios}");
        CloseFile();
        _combat = false; _path = null; _pullAt = 0;
        _windowVersion = -1; _qts = _transition = _decision = "";
        ClearPending();
    }

    public void Shutdown(long now)
    {
        CombatEnded(now, "切换职业/卸载ACR");
        _enabled = false; _closed = true;
        _writes.Writer.TryComplete();
    }

    private void ClearPending()
    {
        // 回调可能正入队，逐项扣减，避免Clear与计数重置交错。
        while (_signals.TryDequeue(out _)) Interlocked.Decrement(ref _signalCount);
        _prepull.Clear(); _seen.Clear(); _requests.Clear();
        _lastRequestId = _castId = 0; _lastRequestAt = 0;
        _stalled = false;
        Interlocked.Exchange(ref _dropped, 0);
    }

    public void ObserveEffect(ulong source, uint id, uint sequence, long now) => Enqueue(false, source, id, sequence, now);
    public void ObserveCast(ulong source, uint id, long now) => Enqueue(true, source, id, 0, now);

    private void Enqueue(bool cast, ulong source, uint id, uint sequence, long now)
    {
        if (!_enabled || source != Volatile.Read(ref _playerId) || id <= 8) return;
        if (Interlocked.Increment(ref _signalCount) > 256)
        {
            Interlocked.Decrement(ref _signalCount); Interlocked.Increment(ref _dropped);
            return;
        }
        _signals.Enqueue((cast, source, id, sequence, now));
    }

    public void Request(uint id, bool off, ReaperState state, string reason)
    {
        if (!_enabled || id == 0) return;
        // 同一候选可能被宿主重复查询；只记录新请求或超时重试。
        if (id == _lastRequestId && state.Now - _lastRequestAt < 1000) return;
        _lastRequestId = id; _lastRequestAt = state.Now;
        _requests.RemoveAll(r => state.Now - r.At > 5000);
        if (_requests.Count >= 64) _requests.RemoveAt(0);
        _requests.Add((++_requestNumber, id, state.Now));
        Record(state.Now, $"请求#{_requestNumber} {(off ? "能力技" : "GCD")}={id} 原因={reason}｜决策前 {Compact(state)}");
    }

    public void Note(long now, string text)
    {
        if (_enabled) Record(now, text);
    }

    public void CancelCountdown()
    {
        if (!_combat) ClearPending();
    }

    public void Frame(ReaperState state, IReaperPlanner planner, string qts, int windowVersion, string window, uint castId)
    {
        _state = state; Volatile.Write(ref _playerId, state.PlayerId);
        if (!_enabled) return;
        _window = window;
        if (state.InCombat && !_combat) CombatStarted(state.Now);
        if (!state.InCombat && _combat) { CombatEnded(state.Now, "脱战"); return; }
        DrainSignals();
        var dropped = Interlocked.Exchange(ref _dropped, 0);
        if (dropped > 0) Record(state.Now, $"缓冲溢出：省略{dropped}条事件/倒数记录");
        if (qts != _qts)
        {
            var changes = _qts.Length == 0 ? qts : string.Join(" ", qts.Split(' ').Except(_qts.Split(' ')));
            _qts = qts; Record(state.Now, "QT " + changes);
        }
        if (windowVersion != _windowVersion)
        {
            _windowVersion = windowVersion;
            Record(state.Now, "窗口 " + window);
        }
        var transition = $"存活={state.Alive} 目标={state.TargetId:X}/{state.HasTarget} 近战={state.Melee} 移动={state.Moving}";
        if (transition != _transition) { _transition = transition; Record(state.Now, "状态 " + transition); }
        if (_castId != 0 && castId == 0) Record(state.Now, $"咏唱结束/中断待效果确认 action={_castId}");
        _castId = castId;
        var decision = $"{planner.ModeName} 路线={planner.RecoveryRoute} 兜底={planner.IsSimple}/{planner.RulesDisabled} QT协调={planner.IsCoordinating} 等大丰收={planner.DeferHarvestQueue} 快速环={state.FastCircle}";
        if (decision != _decision)
        {
            _decision = decision;
            Record(state.Now, $"决策 {decision} 原因={planner.Reason} 建议={planner.GcdAction}/{planner.OffGcdAction} 预计团契={planner.BurstCommunioIn:F2}/完人={planner.BurstFinishIn:F2}｜观察 {Compact(state)}");
        }
        var stalled = state.Alive && state.InCombat && state.HasTarget && state.GcdLeft <= .3f
            && state.Now - _lastEffectAt > Math.Max(3, state.Gcd + .75f) * 1000;
        if (stalled && !_stalled) Record(state.Now, $"未出招 原因={planner.Reason} 建议={planner.GcdAction}/{planner.OffGcdAction}｜观察 {Compact(state)}");
        if (!stalled && _stalled) Record(state.Now, "未出招状态已解除");
        _stalled = stalled;
    }

    private void DrainSignals()
    {
        while (_signals.TryDequeue(out var e))
        {
            Interlocked.Decrement(ref _signalCount);
            if (!_enabled || e.Source != _playerId) continue;
            if (e.Cast) { Record(e.At, $"开始咏唱 action={e.Id}"); continue; }
            if (_seen.Contains((e.Id, e.Sequence))) continue;
            _seen.Enqueue((e.Id, e.Sequence));
            if (_seen.Count > 64) _seen.Dequeue();
            _effects++; _lastEffectAt = e.At;
            if (e.Id == ReaperSkill.团契) { _communios++; if (_state.CircleLeft > 0) _buffCommunios++; }
            if (e.Id == ReaperSkill.完人) { _perfectios++; if (_state.CircleLeft > 0) _buffPerfectios++; }
            if (e.Id == ReaperSkill.暴食) _gluttonies++;
            if (e.Id == ReaperSkill.神秘环) _circles++;
            var match = _requests.FindLastIndex(r => r.Id == e.Id && e.At >= r.At && e.At - r.At <= 5000);
            var request = match >= 0 ? $"匹配请求#{_requests[match].Number}" : "未匹配本ACR请求";
            if (match >= 0) _requests.RemoveAt(match);
            if (_lastRequestId == e.Id) _lastRequestId = 0;
            Record(e.At, $"效果 action={e.Id} seq={e.Sequence} {request}｜框架观察@{(_state.Now - _pullAt) / 1000f:F3}s {Compact(_state)}");
        }
    }

    private void OpenRound(long now, string reason)
    {
        try
        {
            _path ??= Path.Combine(_directory(), $"Reaper_{DateTime.Now:yyyyMMdd-HHmmss-fff}_{Guid.NewGuid().ToString("N")[..6]}.log");
            _writer ??= Task.Run(WriteFiles);
            Record(now, $"{reason}｜{_context}｜player={_state.PlayerId:X}｜{Compact(_state)}");
            if (_qts.Length > 0) Record(now, "QT初始 " + _qts);
            while (_prepull.TryDequeue(out var line))
                if (now - line.At <= 60000) Write(line.At, line.Wall, line.Text);
            Record(now, "窗口初始 " + _window);
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
            if (_prepull.Count >= 96) { _prepull.Dequeue(); Interlocked.Increment(ref _dropped); }
            _prepull.Enqueue((now, DateTime.Now, text));
        }
    }

    private void Write(long now, DateTime wall, string text)
    {
        var line = string.Create(CultureInfo.InvariantCulture, $"{wall:HH:mm:ss.fff} {(now - _pullAt) / 1000d:+0.000;-0.000;0.000}s {text}");
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
        _error = $"Debug记录失败：{ex.GetType().Name} {ex.Message}";
        _enabled = false;
        _writes.Writer.TryComplete();
    }

    private static string Compact(ReaperState s) => string.Create(CultureInfo.InvariantCulture,
        $"红绿={s.Soul}/{s.Shroud} 魂={s.Lemure}/{s.Void} 镰={s.Reavers} 附体={s.Enshrouded:F2} 免费={s.FreeEnshroud:F2} 隐匿={s.Occulta:F2} 完人={s.Perfectio:F2} 祭性={s.Oblatio:F2} 月={s.Soulsow} GCD={s.Gcd:F3}/{s.ReapGcd:F3} 复唱余={s.GcdLeft:F3} 团契复唱={s.CommunioGcd:F3} 团契读条={s.CommunioCast:F3} AC={s.CircleCd:F2}/{s.CircleLeft:F2} 暴食={s.GluttonyCd:F2} 附体CD={s.EnshroudCd:F2} 切割={s.SliceCharges:F3} 烙印={s.DeathDesign:F2} 连击={s.ComboNext}/{s.ComboLeft:F2} 祭品={s.SacrificeStacks}/{s.SacrificeLeft:F2} 解锁={s.Bloodsown:F2} 距离={s.Distance:F2} 移动={s.Moving} 窗口={s.WindowLeft:F2} 留={s.GoalSoul}/{s.GoalShroud}");
}
