using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;


namespace MilkVio.Tank.DRK.Action.Gcd;

public class BaseGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() >= GameData.GetCurrentMeleeRange()) return new CheckResult(false, "当前目标过远（>3m）");
        
        if (Core.Me.DistanceToMe() <= GameData.GetCurrentMeleeRange())
        {
            return new CheckResult(true, "距离 < 3");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if (ActionHelper.GetLastComboID() == DRKSkill.重斩)
        {
            return new PAction(DRKSkill.吸收斩, ActionType.Gcd, ActionTargetType.Target);
        }
        
        if(ActionHelper.GetLastComboID() == DRKSkill.吸收斩)
        {
            return new PAction(DRKSkill.噬魂斩, ActionType.Gcd, ActionTargetType.Target);
        }
        return new PAction(DRKSkill.重斩, ActionType.Gcd, ActionTargetType.Target);
    }
}
