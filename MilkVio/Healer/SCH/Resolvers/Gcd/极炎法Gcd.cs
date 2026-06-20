using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.Gcd;

public sealed class 极炎法Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var targetCheck = HealerUtils.RequireEnemyTarget(25f, 400, false);
        if (!targetCheck.Success) return targetCheck;
        return HealerUtils.Level >= 1 ? HealerUtils.Pass("基础填充") : HealerUtils.Fail("等级不足");
    }

    public PAction GetAction() => HealerUtils.Gcd(SchAction.毁灭, ActionTargetType.Target);
}
