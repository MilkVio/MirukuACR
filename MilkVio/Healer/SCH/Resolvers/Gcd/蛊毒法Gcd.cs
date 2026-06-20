using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.Gcd;

public sealed class 蛊毒法Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var targetCheck = HealerUtils.RequireEnemyTarget(25f, 400);
        if (!targetCheck.Success) return targetCheck;
        return HealerUtils.ShouldSchDot(3f, SchBuff.毒菌, SchBuff.猛毒菌, SchBuff.蛊毒法)
            ? HealerUtils.Pass("续毒")
            : HealerUtils.Fail("DOT时间充足");
    }

    public PAction GetAction() => HealerUtils.Gcd(SchAction.毒菌, ActionTargetType.Target);
}
