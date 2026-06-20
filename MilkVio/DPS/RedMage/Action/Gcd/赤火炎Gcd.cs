using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;
using MilkVio.DPS.RedMage.RDMData;


namespace MilkVio.DPS.RedMage.Action.Gcd;

public class 赤火炎Gcd : IDecisionResolver
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
        var fireStatusLeftTime = StatusHelper.GetStatusLeftTime(me, RDMBuff.赤火炎预备);
        var stoneStatusLeftTime = StatusHelper.GetStatusLeftTime(me, RDMBuff.赤飞石预备);
        if (fireStatusLeftTime < 2.2f) return new CheckResult(false, "打不完这个 放弃");
        
        if (RedMageHelper.IsInManaActionCombo(me)) return new CheckResult(false, "正在魔三连/焦热决断");
        
        if (JobGaugeHelper.RDM.BlackMana - JobGaugeHelper.RDM.WhiteMana + 6 >= 30)
        {
            return new CheckResult(false, "别他妈打黑了 快失衡了");
        }

        
        if (RedMageHelper.HasRedFireReady(me))
        {   
            /*
             * 1.第一种情况 如果自身的这个快过期 <5s了 打这个黑
             * 2.第二种情况 如果自身这个还>8s，但是白<5s 打白
             * 3.第三种情况 如果自身两种都有 且白更低 打白
             */
            if (fireStatusLeftTime < 5) return new CheckResult(true, "快过期了打一个");
            if (fireStatusLeftTime > 8 && RedMageHelper.HasRedStoneReady(me) && stoneStatusLeftTime < 5) return new CheckResult(false, "白快过期了打一个");
            if (RedMageHelper.GetLowestManaType() == ManaType.WhiteMana && RedMageHelper.HasRedStoneReady(me))
            {
                return new CheckResult(false, "平衡魔元打白");
            }
            return new CheckResult(true, "打自己");
        }
        else
        {
            return new CheckResult(false, "当前没有赤火炎预备");
        }
        
        return new CheckResult(true, "打一个");
    }

    public PAction GetAction()
    {
        return new PAction(RDMSkill.赤火炎, ActionType.Gcd, ActionTargetType.Target);
    }
}
