using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.Gcd;

public sealed class 诊断Gcd : IDecisionResolver
{
    private readonly SageRotationContext _context;

    public 诊断Gcd(SageRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        var me = HealerUtils.Me;
        if (me == null) return HealerUtils.Fail("自身未加载");
        if (me.CurrentMp < 900) return HealerUtils.Fail("魔力不足");
        if (!HealerUtils.CanHardCast()) return HealerUtils.Fail("移动中");
        if (!HealerUtils.TryGetLowestParty(35f, out var target)) return HealerUtils.Fail("没有单抬目标");
        _context.SingleHealTarget = target;
        return HealerUtils.Pass("诊断单抬");
    }

    public PAction GetAction() => HealerUtils.Gcd(SgeAction.诊断, _context.SingleHealTarget);
}
