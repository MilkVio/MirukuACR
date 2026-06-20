using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;
using MilkVio.DPS.RedMage.RDMData;


namespace MilkVio.DPS.RedMage.Action.OffGcd;

public class 光芒四射OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        if (RedMageHelper.HasContinuousCast(me)) return new CheckResult(false, "当前正在连续咏唱");
        //if (RedMageHelper.IsInManaActionCombo(me)) return new CheckResult(false, "正在魔三连/焦热决断");
        
        // 自身不可以的条件
        if (RDMSkill.光芒四射.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        if (me.HasStatus(RDMBuff.光芒四射预备)) return new CheckResult(true, "打一个");
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(RDMSkill.光芒四射, ActionType.OffGcd, ActionTargetType.Target);
    }
}
