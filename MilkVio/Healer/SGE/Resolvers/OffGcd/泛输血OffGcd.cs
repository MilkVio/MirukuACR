using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 泛输血OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        if (HealerUtils.LowPartyCount(72f) < 3) return HealerUtils.Fail("群体血量安全");
        return HealerUtils.IsReady(SgeAction.泛输血, 80) ? HealerUtils.Pass("泛输血") : HealerUtils.Fail("泛输血未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SgeAction.泛输血, ActionTargetType.Self);
}
