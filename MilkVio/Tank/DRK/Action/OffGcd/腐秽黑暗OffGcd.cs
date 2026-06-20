using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK.Action.OffGcd;

public class 腐秽黑暗OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() >= 5f) return new CheckResult(false, "当前目标过远（>5m）");
        
        if (Core.Me.HasStatus(DRKBuff.腐秽大地) && DRKSkill.腐秽黑暗.GetActionCooldown() == 0 && Core.Me.Level >= 86)
        {
            return new CheckResult(true, "有腐秽黑暗发动条件");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(DRKSkill.腐秽黑暗, ActionType.OffGcd, ActionTargetType.Self);
    }
}
