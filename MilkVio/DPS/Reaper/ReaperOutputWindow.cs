using System.Globalization;
using System.Text.RegularExpressions;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Reaper;

public enum ReaperWindowEvent { 开始咏唱, 技能效果, 天气变化 }

public sealed record ReaperWindowSettings
{
    public float Seconds { get; init; } = 120;
    public bool KeepResources { get; init; }
    public int Soul { get; init; } = 50;
    public int Shroud { get; init; } = 50;
    public bool EndOnTime { get; init; } = true;
    public bool EndWithoutEnemies { get; init; }
    public bool EndOnEvent { get; init; }
    public ReaperWindowEvent Event { get; init; }
    public string Match { get; init; } = "";
    public float Before { get; init; } = 10;
    public float After { get; init; } = 10;
}

// 时间轴要求不随一次技能计划取消；事件回调只记录信号，由更新处理结束。
public sealed class ReaperOutputWindow
{
    private const string SourceKey = "MilkVio.Reaper.OutputWindow";
    private readonly object _gate = new();
    private ReaperWindowSettings? _settings;
    private ReaperWindowSettings? _lastSettings;
    private SharedBlackboard? _source;
    private string _sourceToken = "";
    private Regex? _match;
    private uint _weather;
    private long _start, _expected, _detect, _limit, _matchedAt = -1;
    private long _emptyAt = -1;
    private int _version;
    private DateTime _publishedUtc;
    public string LastReason { get; private set; } = "未设置窗口";
    internal Action<long, string>? Changed { get; set; }

    public int Version { get { lock (_gate) return _version; } }
    public bool Active { get { lock (_gate) return _settings != null; } }
    public bool IsSimulation { get { lock (_gate) return _settings != null && _source == null; } }

    public bool Set(ReaperWindowSettings settings, SharedBlackboard? source, long now, out string error)
    {
        error = "";
        if (!float.IsFinite(settings.Seconds) || settings.Seconds <= 0 || settings.Seconds > 86400
            || !float.IsFinite(settings.Before) || settings.Before < 0 || settings.Before > 86400
            || !float.IsFinite(settings.After) || settings.After < 0 || settings.After > 86400)
            error = "时间需为有效秒数，总时长大于0，单项不超过86400秒";
        else if (settings.Soul is < 0 or > 100 || settings.Shroud is < 0 or > 100)
            error = "红绿目标必须在0～100";
        else if (!settings.EndOnTime && !settings.EndWithoutEnemies && !settings.EndOnEvent)
            error = "至少选择一种结束方式";
        Regex? match = null;
        uint weather = 0;
        if (error.Length == 0 && settings.EndOnEvent)
        {
            if (!Enum.IsDefined(settings.Event) || string.IsNullOrWhiteSpace(settings.Match)) error = "请填写结束事件";
            else if (settings.Event == ReaperWindowEvent.天气变化)
            {
                if (!uint.TryParse(settings.Match, out weather)) error = "天气ID必须是非负整数";
            }
            else
            {
                try
                {
                    var pattern = settings.Match.Trim();
                    if (pattern.Length > 512) throw new ArgumentException();
                    if (Regex.IsMatch(pattern, @"^\d+(\s*\|\s*\d+)*$")) pattern = "^(?:" + string.Join('|', pattern.Split('|').Select(p => p.Trim())) + ")$";
                    match = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(5));
                }
                catch (ArgumentException) { error = "ActionId正则无效或过长"; }
            }
        }
        if (error.Length != 0) return false;
        lock (_gate)
        {
            ClearSource();
            _version++;
            _settings = settings;
            _lastSettings = settings;
            _publishedUtc = DateTime.UtcNow;
            _source = source;
            _sourceToken = Guid.NewGuid().ToString("N");
            if (_source != null) _source.Strings[SourceKey] = _sourceToken;
            _match = match; _weather = weather;
            _start = now;
            _expected = now + (long)(settings.Seconds * 1000);
            _detect = Math.Max(now, _expected - (long)(settings.Before * 1000));
            _limit = _expected + (settings.EndOnEvent || settings.EndWithoutEnemies ? (long)(settings.After * 1000) : 0);
            _matchedAt = _emptyAt = -1;
            LastReason = "窗口已设置";
            Changed?.Invoke(now, $"窗口设置 来源={(source == null ? "手动模拟" : "时间轴")} {DebugDetails()}");
        }
        return true;
    }

    public void Observe(ReaperWindowEvent kind, uint actionId, long now, DateTime? eventTime = null)
    {
        lock (_gate)
        {
            if (_settings is not { EndOnEvent: true } settings || settings.Event != kind
                || now < _detect || now > _limit || _matchedAt >= 0 || _match == null) return;
            if (eventTime.HasValue && eventTime.Value.ToUniversalTime() < _publishedUtc) return;
            try
            {
                if (_match.IsMatch(actionId.ToString(CultureInfo.InvariantCulture))) _matchedAt = now;
            }
            catch (RegexMatchTimeoutException) { _match = null; LastReason = "结束正则超时，等待窗口上限"; }
        }
    }

    public ReaperState Update(ReaperState state, bool? noEnemies = null, uint? weather = null)
    {
        state = state with { WindowActive = false, WindowVersion = 0, WindowLeft = 0, WindowLimit = 0, GoalSoul = 0, GoalShroud = 0 };
        lock (_gate)
        {
            if (_settings == null) return state;
            if (_source != null && (!_source.Strings.TryGetValue(SourceKey, out var token) || token != _sourceToken))
            { Clear("时间轴运行已结束"); return state; }
            var now = state.Now;
            if (now < _start) { Clear("时钟已重置"); return state; }
            if (now >= _detect)
            {
                if (_settings.EndOnEvent)
                {
                    if (_settings.Event == ReaperWindowEvent.天气变化 && weather == _weather && now <= _limit) _matchedAt = now;
                    if (_matchedAt >= _detect && _matchedAt <= _limit)
                    { Clear($"{_settings.Event}匹配，窗口结束"); return state; }
                }
                else if (_settings.EndWithoutEnemies)
                {
                    if (noEnemies == true && state.Alive && state.PlayerId != 0)
                    {
                        if (_emptyAt >= 0 && now > _emptyAt) { Clear("周围无可攻击敌人，窗口结束"); return state; }
                        _emptyAt = now;
                    }
                    else _emptyAt = -1;
                }
            }
            if (now >= _limit) { Clear("到达结束上限"); return state; }
            return state with
            {
                WindowActive = true, WindowVersion = _version,
                WindowLeft = Math.Max(0, (_expected - now) / 1000f),
                WindowLimit = Math.Max(0, (_limit - now) / 1000f),
                GoalSoul = _settings.KeepResources ? _settings.Soul : 0,
                GoalShroud = _settings.KeepResources ? _settings.Shroud : 0,
                DumpQt = false
            };
        }
    }

    public void Clear(string reason = "手动清除窗口")
    {
        lock (_gate)
        {
            if (_settings != null) Changed?.Invoke(Environment.TickCount64, $"窗口结束 {reason} {DebugDetails()}");
            ClearSource();
            _settings = null; _source = null; _match = null;
            _matchedAt = _emptyAt = -1;
            _version++; LastReason = reason;
        }
    }

    public void Reset(string reason)
    {
        lock (_gate)
        {
            Clear(reason);
            _lastSettings = null;
            _sourceToken = "";
            _start = _expected = _detect = _limit = 0;
            _weather = 0;
            _publishedUtc = default;
        }
    }

    private void ClearSource()
    {
        if (_source != null && _source.Strings.TryGetValue(SourceKey, out var token) && token == _sourceToken)
            _source.Strings.Remove(SourceKey);
    }

    public string Describe(long now)
    {
        lock (_gate)
        {
            if (_settings == null) return LastReason;
            var source = _source == null ? "手动模拟" : "时间轴";
            var mode = _settings.EndOnEvent ? _settings.Event.ToString() : _settings.EndWithoutEnemies ? "无可攻击敌人" : "到时结束";
            var reserve = _settings.KeepResources ? $"留红{_settings.Soul}/绿{_settings.Shroud}" : "不设期末资源";
            var timing = _settings.EndOnEvent || _settings.EndWithoutEnemies
                ? $"｜{(now < _detect ? $"{(_detect - now) / 1000f:F1}s后开始检测" : "检测中")}｜最晚 {Math.Max(0, (_limit - now) / 1000f):F1}s"
                : "";
            return $"{source}｜窗口剩余 {Math.Max(0, (_expected - now) / 1000f):F1}s｜{reserve}\n结束：{mode}{timing}";
        }
    }

    public (bool Enemies, bool Weather) DetectionNeeded(long now)
    {
        lock (_gate)
        {
            if (_settings == null || now < _detect || now > _limit) return (false, false);
            return (!_settings.EndOnEvent && _settings.EndWithoutEnemies,
                _settings.EndOnEvent && _settings.Event == ReaperWindowEvent.天气变化);
        }
    }

    public string DebugDetails()
    {
        lock (_gate) return $"窗口版本={_version} 生效={_start} 预计={_expected} 检测开始={_detect} 强制结束={_limit}\n{_lastSettings}\n{LastReason}";
    }
}
