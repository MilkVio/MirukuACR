using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 输血OffGcd : IDecisionResolver
{
    private readonly SageRotationContext _context;

    public 输血OffGcd(SageRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        if (!HealerUtils.TryGetLowestTank(62f, out var target)) return HealerUtils.Fail("没有输血目标");
        _context.SingleHealTarget = target;
        return HealerUtils.IsReady(SgeAction.输血, 70) ? HealerUtils.Pass("输血") : HealerUtils.Fail("输血未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SgeAction.输血, _context.SingleHealTarget);
}
