using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.GNB.GNBData;

namespace MilkVio.Tank.GNB.Action.Gcd;

public class 狮心连Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未初始化");

        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (me.DistanceToMe() <= currentMeleeRange)
        {
            if (ActionHelper.GetAdjustedActionId(GNBSkill.血壤) != GNBSkill.血壤)
            {
                return new CheckResult(true, "存在狮心状态");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if(ActionHelper.GetAdjustedActionId(GNBSkill.血壤) == GNBSkill.支配之心) return new PAction(GNBSkill.支配之心, ActionType.Gcd, ActionTargetType.Target);
        if(ActionHelper.GetAdjustedActionId(GNBSkill.血壤) == GNBSkill.终结之心) return new PAction(GNBSkill.终结之心, ActionType.Gcd, ActionTargetType.Target);
        return new PAction(GNBSkill.崛起之心, ActionType.Gcd, ActionTargetType.Target);
    }
}
