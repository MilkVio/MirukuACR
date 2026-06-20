using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.Gcd;

public sealed class 魂灵风息Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.魂灵风息)) return HealerUtils.Fail("魂灵风息QT关闭");
        if (HealerUtils.Level < 90) return HealerUtils.Fail("等级不足");
        var targetCheck = HealerUtils.RequireEnemyTarget(25f, 700, false);
        if (!targetCheck.Success) return targetCheck;
        if (!HealerUtils.Qt(HealerQt.爆发) && HealerUtils.LowPartyCount(80f) < 2) return HealerUtils.Fail("保留魂灵风息");
        return HealerUtils.Pass("魂灵风息");
    }

    public PAction GetAction() => HealerUtils.Gcd(SgeAction.魂灵风息, ActionTargetType.Target);
}
