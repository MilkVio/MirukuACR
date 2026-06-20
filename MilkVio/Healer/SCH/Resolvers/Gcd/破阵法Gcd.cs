using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.Gcd;

public sealed class 破阵法Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.AOE)) return HealerUtils.Fail("AOE QT关闭");
        if (HealerUtils.Level < 46) return HealerUtils.Fail("等级不足");
        var targetCheck = HealerUtils.RequireEnemyTarget(5f, 600);
        if (!targetCheck.Success) return targetCheck;
        return HealerUtils.EnemyCountAroundSelf(5f) >= 2 ? HealerUtils.Pass("破阵AOE") : HealerUtils.Fail("AOE目标不足");
    }

    public PAction GetAction() => HealerUtils.Gcd(SchAction.破阵法, ActionTargetType.Self);
}
