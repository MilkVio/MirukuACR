using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist;

// 只观察实际公共复唱，不在选招或 Buff 刚出现时提前限制能力技。
internal sealed class MachinistWeaveLimiter
{
    private const int SnapshotGraceMs = 100;
    private bool _limited;
    private bool _hadOverheat;
    private float _lastOverheatLeft;
    private long _overheatUntil;
    private bool _hadShortGcd;
    private uint _lastGcdAction;
    private float _lastGcdElapsed;
    private long _shortGcdUntil;

    public int Update(long now, bool overheated, float overheatLeft,
        bool gcdActive, uint gcdAction, float gcdTotal, float gcdElapsed)
    {
        var hasOverheat = overheated && float.IsFinite(overheatLeft) && overheatLeft > 0;
        if (hasOverheat)
        {
            // 正常倒计时只能缩短截止时间；旧 Buff 快照不能每帧给自己续期。
            var until = now + (long)Math.Ceiling(Math.Min(overheatLeft, 30) * 1000) + SnapshotGraceMs;
            if (!_hadOverheat || overheatLeft > _lastOverheatLeft + 0.1f)
                _overheatUntil = until;
            else
                _overheatUntil = Math.Min(_overheatUntil, until);
        }
        else
        {
            _overheatUntil = 0;
        }
        _hadOverheat = hasOverheat;
        _lastOverheatLeft = hasOverheat ? overheatLeft : 0;

        var shortGcd = gcdActive
                       && gcdAction is MCHSkill.热冲击 or MCHSkill.烈焰弹 or MCHSkill.自动弩
                       && float.IsFinite(gcdTotal) && gcdTotal > 0 && gcdTotal <= 1.6f
                       && float.IsFinite(gcdElapsed) && gcdElapsed >= 0 && gcdElapsed < gcdTotal;
        if (shortGcd)
        {
            // 复唱推进或新一发热冲击才更新尾窗，静止的旧数据超时后不会重新锁成 1。
            if (!_hadShortGcd || gcdAction != _lastGcdAction || gcdElapsed != _lastGcdElapsed)
                _shortGcdUntil = now + (long)Math.Ceiling((gcdTotal - gcdElapsed) * 1000) + SnapshotGraceMs;
        }
        else
        {
            _shortGcdUntil = 0;
        }
        _hadShortGcd = shortGcd;
        _lastGcdAction = gcdAction;
        _lastGcdElapsed = gcdElapsed;

        var inShortGcd = shortGcd && now < _shortGcdUntil;
        if (!hasOverheat || now >= _overheatUntil)
            _limited = false;
        else if (inShortGcd)
            _limited = true;

        // 最后一发耗尽 Buff 后仍保护它留下的 1.5 秒；不能在进入输入窗口时提前恢复。
        return _limited || inShortGcd ? 1 : 2;
    }

    public void Reset()
    {
        _limited = _hadOverheat = _hadShortGcd = false;
        _lastOverheatLeft = _lastGcdElapsed = 0;
        _overheatUntil = _shortGcdUntil = 0;
        _lastGcdAction = 0;
    }
}
