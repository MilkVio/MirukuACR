using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.Gcd;

public sealed class 均衡失衡Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.AOE)) return HealerUtils.Fail("AOE QT关闭");
        if (!HealerUtils.SgeEukrasia()) return HealerUtils.Fail("没有均衡");
        if (HealerUtils.EnemyCountAroundSelf(5f) < 3) return HealerUtils.Fail("AOE目标不足");

        var targetCheck = HealerUtils.RequireEnemyTarget(5f, 400);
        if (!targetCheck.Success) return targetCheck;

        if (!HealerUtils.ShouldRefreshOwnTargetStatusBelow(2.5f, SgeBuff.均衡失衡))
            return HealerUtils.Fail("均衡失衡时间充足");

        return HealerUtils.Level >= 82 ? HealerUtils.Pass("均衡失衡续毒") : HealerUtils.Fail("等级不足");
    }

    public PAction GetAction() => HealerUtils.Gcd(SgeAction.均衡失衡, ActionTargetType.Self);
}
