using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 寄生橡汁OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        if (HealerUtils.SgeAddersgall() == 0) return HealerUtils.Fail("没有蛇胆");
        if (HealerUtils.LowPartyCount(72f) < 3) return HealerUtils.Fail("群体血量安全");
        return HealerUtils.IsReady(SgeAction.寄生橡汁, 52) ? HealerUtils.Pass("寄生橡汁群抬") : HealerUtils.Fail("寄生橡汁未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SgeAction.寄生橡汁, ActionTargetType.Self);
}
