using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;
using MilkVio.DPS.RedMage.RDMData;


namespace MilkVio.DPS.RedMage.Action.Gcd;

public class 赤飞石Gcd : IDecisionResolver
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
        
        // 决策不允许的条件
        var stoneStatusLeftTime = StatusHelper.GetStatusLeftTime(me, RDMBuff.赤飞石预备);
        if (stoneStatusLeftTime < 2.2f) return new CheckResult(false, "打不完这个 放弃");
        
        if (RedMageHelper.IsInManaActionCombo(me)) return new CheckResult(false, "正在魔三连/焦热决断");
        if (!RedMageHelper.HasRedStoneReady(me)) return new CheckResult(false, "当前没有赤飞石预备");
        
        return new CheckResult(true, "打一个");
    }

    public PAction GetAction()
    {
        return new PAction(RDMSkill.赤飞石, ActionType.Gcd, ActionTargetType.Target);
    }
}
