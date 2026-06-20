using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 心神风息OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.心神风息)) return HealerUtils.Fail("心神风息QT关闭");
        var targetCheck = HealerUtils.RequireEnemyTarget(25f);
        if (!targetCheck.Success) return targetCheck;
        return HealerUtils.IsReady(SgeAction.心神风息, 92) ? HealerUtils.Pass("心神风息") : HealerUtils.Fail("心神风息未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SgeAction.心神风息, ActionTargetType.Target);
}
