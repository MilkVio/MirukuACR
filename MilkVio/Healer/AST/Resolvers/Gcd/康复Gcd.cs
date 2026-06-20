using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.Gcd;

public sealed class 康复Gcd : IDecisionResolver
{
    private readonly AstrologianRotationContext _context;

    public 康复Gcd(AstrologianRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.康复)) return HealerUtils.Fail("康复QT关闭");
        if (HealerUtils.IsMovingWithoutInstant()) return HealerUtils.Fail("移动中且无即刻/光速");
        if (!HealerUtils.TryGetCleanseTarget(out var target)) return HealerUtils.Fail("没有可驱散目标");

        _context.SingleHealTarget = target;
        return HealerUtils.Pass("康复");
    }

    public PAction GetAction() => HealerUtils.Gcd(RoleAction.康复, _context.SingleHealTarget);
}
