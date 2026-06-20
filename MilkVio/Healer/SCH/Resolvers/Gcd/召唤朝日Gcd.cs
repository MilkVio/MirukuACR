using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.Gcd;

public sealed class 召唤朝日Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.朝日召唤)) return HealerUtils.Fail("朝日召唤QT关闭");
        if (HealerUtils.Level < 4) return HealerUtils.Fail("等级不足");

        var me = HealerUtils.Me;
        if (me == null) return HealerUtils.Fail("自身未加载");
        if (HealerUtils.HpPercent(me) < 10f) return HealerUtils.Fail("自身血量过低");
        if (HealerUtils.SchInDissipationOrGrace()) return HealerUtils.Fail("转化保护窗口内不召唤仙女");
        var hasPet = HealerUtils.SchHasPet();
        if (hasPet == true) return HealerUtils.Fail("朝日已在场");
        if (hasPet == null) return HealerUtils.Fail("Prome暂未提供SCH宠物状态");
        if (HealerUtils.SchSeraphTimer() > 0) return HealerUtils.Fail("炽天使期间不召唤朝日");
        if (HealerUtils.IsMovingWithoutInstant()) return HealerUtils.Fail("移动中且无即刻");

        return HealerUtils.Pass("召唤朝日");
    }

    public PAction GetAction() => HealerUtils.Gcd(SchAction.朝日召唤, ActionTargetType.Self);
}
