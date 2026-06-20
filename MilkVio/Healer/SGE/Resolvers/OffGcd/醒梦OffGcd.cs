using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 醒梦OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.醒梦)) return HealerUtils.Fail("醒梦QT关闭");
        var me = HealerUtils.Me;
        if (me == null) return HealerUtils.Fail("自身未加载");
        if (me.CurrentMp >= 7000) return HealerUtils.Fail("蓝量充足");
        return HealerUtils.IsReady(RoleAction.醒梦, 24) ? HealerUtils.Pass("魔力不足") : HealerUtils.Fail("醒梦未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(RoleAction.醒梦, ActionTargetType.Self);
}
