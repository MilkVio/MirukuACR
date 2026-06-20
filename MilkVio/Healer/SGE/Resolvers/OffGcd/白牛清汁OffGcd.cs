using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 白牛清汁OffGcd : IDecisionResolver
{
    private readonly SageRotationContext _context;

    public 白牛清汁OffGcd(SageRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        if (HealerUtils.SgeAddersgall() == 0) return HealerUtils.Fail("没有蛇胆");
        if (!HealerUtils.TryGetLowestParty(55f, out var target)) return HealerUtils.Fail("没有白牛目标");
        _context.SingleHealTarget = target;
        return HealerUtils.IsReady(SgeAction.白牛清汁, 62) ? HealerUtils.Pass("白牛清汁") : HealerUtils.Fail("白牛清汁未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SgeAction.白牛清汁, _context.SingleHealTarget);
}
