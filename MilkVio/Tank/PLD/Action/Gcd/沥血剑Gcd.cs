using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Action.Gcd;

public class 沥血剑Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, "当前目标距离过远");
        
        if (Core.Me.DistanceToMe() <= currentMeleeRange && Core.Me.HasStatus(PLDBuff.沥血剑预备))
        {
            return new CheckResult(true, "距离 < 3");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PLDSkill.沥血剑, ActionType.Gcd, ActionTargetType.Target);
    }
}
