using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.OffGcd;

public sealed class 擢升OffGcd : IDecisionResolver
{
    private readonly AstrologianRotationContext _context;

    public 擢升OffGcd(AstrologianRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        if (!HealerUtils.TryGetLowestParty(46f, out var target)) return HealerUtils.Fail("没有单抬目标");
        _context.SingleHealTarget = target;
        return HealerUtils.HasCharge(AstAction.擢升, 15) ? HealerUtils.Pass("擢升单抬") : HealerUtils.Fail("擢升无层数");
    }

    public PAction GetAction() => HealerUtils.OffGcd(AstAction.擢升, _context.SingleHealTarget);
}
