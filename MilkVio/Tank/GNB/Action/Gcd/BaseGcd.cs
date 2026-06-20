using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.GNB.GNBData;

namespace MilkVio.Tank.GNB.Action.Gcd;

public class BaseGcd : IDecisionResolver
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
            return new CheckResult(true, "距离 < 3");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if (ActionHelper.GetLastComboID() == GNBSkill.利刃斩)
        {
            return new PAction(GNBSkill.残暴弹, ActionType.Gcd, ActionTargetType.Target);
        }
        
        if(ActionHelper.GetLastComboID() == GNBSkill.残暴弹)
        {
            return new PAction(GNBSkill.迅连斩, ActionType.Gcd, ActionTargetType.Target);
        }
        return new PAction(GNBSkill.利刃斩, ActionType.Gcd, ActionTargetType.Target);
    }
}
