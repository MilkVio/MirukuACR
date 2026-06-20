using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;


namespace MilkVio.DPS.RedMage.Action.Gcd;

public class 震荡Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        // 自身不可以的条件
        if (RedMageHelper.HasContinuousCast(me)) return new CheckResult(false, "当前有连续咏唱");
        if (MoveManager.IsLocalPlayerMoving) return new CheckResult(false, "当前正在移动"); 
        
        
        
        return new CheckResult(true, "打一个");
    }

    public PAction GetAction()
    {
        return new PAction(RedMageHelper.GetCurrentShakeActionId(Core.Me), ActionType.Gcd, ActionTargetType.Target);
    }
}
