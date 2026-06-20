using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.OffGcd;

public sealed class 即刻极炎法OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.即刻极炎法)) return HealerUtils.Fail("即刻极炎法QT关闭");
        if (!MoveManager.IsLocalPlayerMoving) return HealerUtils.Fail("未移动");

        var targetCheck = HealerUtils.RequireEnemyTarget(25f, 400);
        if (!targetCheck.Success) return targetCheck;

        var me = HealerUtils.Me;
        if (me == null) return HealerUtils.Fail("自身未加载");
        if (me.HasStatus(RoleBuff.即刻咏唱)) return HealerUtils.Fail("已有即刻");

        return HealerUtils.IsReady(RoleAction.即刻咏唱, 18)
            ? HealerUtils.Pass("移动即刻极炎法")
            : HealerUtils.Fail("即刻不可用");
    }

    public PAction GetAction() => HealerUtils.OffGcd(RoleAction.即刻咏唱, ActionTargetType.Self);
}
