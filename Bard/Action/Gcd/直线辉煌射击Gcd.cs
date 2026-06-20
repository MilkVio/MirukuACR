using Bard.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace Bard.Action.Gcd;

public class 直线辉煌射击Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var player = PromeRotation.Core.Core.Me;
        if (player == null) return new CheckResult(false, "玩家自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == player.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (player.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        if (player.HasStatus(BardBuff.鹰眼Buff) || player.HasStatus(BardBuff.纷乱箭Buff))
            return new CheckResult(true, $"有Buff 射击");
        
        return new CheckResult(false, "当前不满足任何条件");
    }
    
    public PAction GetAction()
    {
        return new PAction(BardHelper.GetStraightShootActionCurrentId(), ActionType.Gcd, ActionTargetType.Target);
    }
}
