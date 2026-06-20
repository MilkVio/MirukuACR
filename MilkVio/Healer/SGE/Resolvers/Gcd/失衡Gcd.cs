using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.Gcd;

public sealed class 失衡Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.AOE)) return HealerUtils.Fail("AOE QT关闭");
        if (HealerUtils.Level < 46) return HealerUtils.Fail("等级不足");
        var targetCheck = HealerUtils.RequireEnemyTarget(5f, 400);
        if (!targetCheck.Success) return targetCheck;
        return HealerUtils.EnemyCountAroundSelf(5f) >= 3 ? HealerUtils.Pass("失衡AOE") : HealerUtils.Fail("AOE目标不足");
    }

    public PAction GetAction() => HealerUtils.Gcd(SgeAction.失衡, ActionTargetType.Self);
}
