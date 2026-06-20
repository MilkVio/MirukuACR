using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.Gcd;

public sealed class 发炎Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.发炎)) return HealerUtils.Fail("发炎QT关闭");
        var targetCheck = HealerUtils.RequireEnemyTarget(6f);
        if (!targetCheck.Success) return targetCheck;
        return HealerUtils.HasCharge(SgeAction.发炎, 26) ? HealerUtils.Pass("发炎") : HealerUtils.Fail("发炎无层数");
    }

    public PAction GetAction() => HealerUtils.Gcd(SgeAction.发炎, ActionTargetType.Target);
}
