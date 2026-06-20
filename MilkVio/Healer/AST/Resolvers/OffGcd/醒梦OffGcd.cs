using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.OffGcd;

public sealed class 醒梦OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 醒梦是资源兜底 oGCD，注册顺序放最后，避免抢占爆发窗口动作。
        if (!HealerUtils.Qt(HealerQt.醒梦)) return HealerUtils.Fail("醒梦QT关闭");

        var me = HealerUtils.Me;
        if (me == null) return HealerUtils.Fail("自身未加载");

        // 蓝量高于 8000 时保留醒梦；低于等于 8000 才尝试使用。
        if (me.CurrentMp > 8000) return HealerUtils.Fail("蓝量充足");

        // 24 级习得醒梦，CD 转好后执行。
        return HealerUtils.IsReady(RoleAction.醒梦, 24) ? HealerUtils.Pass("魔力不足") : HealerUtils.Fail("醒梦未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(RoleAction.醒梦, ActionTargetType.Self);
}
