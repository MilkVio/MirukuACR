using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 哲学OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗) && !HealerUtils.Qt(HealerQt.爆发)) return HealerUtils.Fail("治疗和爆发QT均关闭");
        if (HealerUtils.LowPartyCount(75f) < 3 && !HealerUtils.Qt(HealerQt.爆发)) return HealerUtils.Fail("没有哲学需求");
        return HealerUtils.IsReady(SgeAction.哲学, 100) ? HealerUtils.Pass("哲学") : HealerUtils.Fail("哲学未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SgeAction.哲学, ActionTargetType.Self);
}
