namespace MilkVio.DPS.Machinist.Planning;

// 只保留日志中的团辅观察时间，不参与资源规划。
internal sealed class MachinistPartyClock
{
    private long _start, _last;
    private float _left;
    private ulong _target;

    internal void Reset() { _start = _last = 0; _left = 0; _target = 0; }

    internal void Observe(MachinistState s)
    {
        if (!s.Alive || !s.HasTarget || _target != 0 && _target != s.TargetId) Reset();
        if (!s.Alive || !s.HasTarget) return;
        _target = s.TargetId;
        var expected = Math.Max(0, _left - (s.Now - _last) / 1000f);
        // 增益自然倒计时不重设锚点；延迟出现的队友团辅可重新校准。
        if (s.PartyLeft > 0 && (_start == 0 || s.PartyLeft > expected + 1))
            _start = s.Now - (long)(Math.Max(0, 20 - s.PartyLeft) * 1000);
        _left = s.PartyLeft; _last = s.Now;
    }

    internal float? StartIn(long now) => _start != 0 && now - _start < 150000 ? (_start - now) / 1000f : null;
}
