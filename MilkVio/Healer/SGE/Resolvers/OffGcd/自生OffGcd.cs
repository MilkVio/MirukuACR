using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 自生OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        if (HealerUtils.LowPartyCount(82f) < 3) return HealerUtils.Fail("群体血量安全");
        return HealerUtils.IsReady(SgeAction.自生, 20) ? HealerUtils.Pass("自生") : HealerUtils.Fail("自生未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SgeAction.自生, ActionTargetType.Self);
}
