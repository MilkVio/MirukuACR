using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 根素OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        if (HealerUtils.SgeAddersgall() > 0) return HealerUtils.Fail("仍有蛇胆");
        if (HealerUtils.LowPartyCount(75f) < 2 && !HealerUtils.TryGetLowestParty(55f, out _)) return HealerUtils.Fail("暂不需要根素");
        return HealerUtils.IsReady(SgeAction.根素, 74) ? HealerUtils.Pass("根素") : HealerUtils.Fail("根素未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SgeAction.根素, ActionTargetType.Self);
}
