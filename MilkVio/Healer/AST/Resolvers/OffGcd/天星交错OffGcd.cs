using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.OffGcd;

public sealed class 天星交错OffGcd : IDecisionResolver
{
    private readonly AstrologianRotationContext _context;

    public 天星交错OffGcd(AstrologianRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        if (!HealerUtils.TryGetLowestParty(72f, out var target)) return HealerUtils.Fail("没有护盾目标");
        _context.SingleHealTarget = target;
        return HealerUtils.HasCharge(AstAction.天星交错, 74) ? HealerUtils.Pass("天星交错") : HealerUtils.Fail("天星交错无层数");
    }

    public PAction GetAction() => HealerUtils.OffGcd(AstAction.天星交错, _context.SingleHealTarget);
}
