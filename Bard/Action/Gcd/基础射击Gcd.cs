using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace Bard.Action.Gcd;

public class 基础射击Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var player = Core.Me;
        if (player == null) return new CheckResult(false, "玩家自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == player.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (player.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        return new CheckResult(true, $"距离 <= {currentAttackRange}");
    }
    
    public PAction GetAction()
    {
        return new PAction(BardHelper.GetNormalShootActionCurrentId(), ActionType.Gcd, ActionTargetType.Target);
    }
}
