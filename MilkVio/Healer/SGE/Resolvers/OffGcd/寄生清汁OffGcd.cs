using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 寄生清汁OffGcd : IDecisionResolver
{
    private readonly SageRotationContext _context;

    public 寄生清汁OffGcd(SageRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        if (HealerUtils.SgeAddersgall() == 0) return HealerUtils.Fail("没有蛇胆");
        if (!HealerUtils.TryGetLowestParty(45f, out var target)) return HealerUtils.Fail("没有寄生清汁目标");
        _context.SingleHealTarget = target;
        return HealerUtils.IsReady(SgeAction.寄生清汁, 45) ? HealerUtils.Pass("寄生清汁") : HealerUtils.Fail("寄生清汁未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SgeAction.寄生清汁, _context.SingleHealTarget);
}
