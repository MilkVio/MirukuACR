using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 坚角清汁OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        if (HealerUtils.SgeAddersgall() == 0) return HealerUtils.Fail("没有蛇胆");
        if (HealerUtils.LowPartyCount(85f) < 3) return HealerUtils.Fail("群体血量安全");
        return HealerUtils.IsReady(SgeAction.坚角清汁, 50) ? HealerUtils.Pass("坚角清汁") : HealerUtils.Fail("坚角清汁未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SgeAction.坚角清汁, ActionTargetType.Self);
}
