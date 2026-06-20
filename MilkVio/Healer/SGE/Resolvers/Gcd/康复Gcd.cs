using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.Gcd;

public sealed class 康复Gcd : IDecisionResolver
{
    private readonly SageRotationContext _context;

    public 康复Gcd(SageRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.康复)) return HealerUtils.Fail("康复QT关闭");
        if (!HealerUtils.TryGetCleanseTarget(out var target)) return HealerUtils.Fail("没有可康复目标");
        if (!HealerUtils.CanHardCast()) return HealerUtils.Fail("移动中");

        _context.CleanseTarget = target;
        return HealerUtils.Level >= 10 ? HealerUtils.Pass("康复可解除状态") : HealerUtils.Fail("等级不足");
    }

    public PAction GetAction() => HealerUtils.Gcd(RoleAction.康复, _context.CleanseTarget);
}
