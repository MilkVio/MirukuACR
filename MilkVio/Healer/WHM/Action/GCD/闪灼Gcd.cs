using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.WHM.Action.GCD;

public class 闪灼Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (me.CurrentMp < 400) return new CheckResult(false, "魔力不足");
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        if (MoveManager.IsLocalPlayerMoving) return new CheckResult(false, "当前正在移动");
        
        return new CheckResult(true, "打一个");
    }

    public PAction GetAction()
    {
        return new PAction(WhiteMageHelper.GetCurrent闪灼(), ActionType.Gcd, ActionTargetType.Target);
    }
}
