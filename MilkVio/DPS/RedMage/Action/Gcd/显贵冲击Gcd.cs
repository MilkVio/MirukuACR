using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;
using MilkVio.DPS.RedMage.RDMData;


namespace MilkVio.DPS.RedMage.Action.Gcd;

public class 显贵冲击Gcd : IDecisionResolver
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
        //if (MoveManager.IsLocalPlayerMoving) return new CheckResult(false, "当前正在移动"); 
        
        // 决策不允许的条件
        if (!me.HasStatus(RDMBuff.显贵冲击预备)) return new CheckResult(false, "未获得显贵冲击Buff");
        if (RedMageHelper.IsInManaActionCombo(me)) return new CheckResult(false, "正在魔三连/焦热决断");

        if (me.HasStatus(RDMBuff.鼓励))
        {
            return new CheckResult(true, "团副内直接打");
        }

        if (StatusHelper.GetStatusLeftTime(me, RDMBuff.显贵冲击预备) < 5)
        {
            return new CheckResult(true, "快过期直接打");
        }
        
        if (MoveManager.IsLocalPlayerMoving) return new CheckResult(true, "正在移动直接打"); 
        return new CheckResult(false, "不满足任何条件"); 
    }

    public PAction GetAction()
    {
        return new PAction(RDMSkill.显贵冲击, ActionType.Gcd, ActionTargetType.Target);
    }
}
