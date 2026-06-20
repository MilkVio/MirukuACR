using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.RedMage.RDMData;

namespace MilkVio.DPS.RedMage.Action.Gcd;

public class 促进赤风_单体Gcd: IDecisionResolver
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
        if (me.HasStatus(RDMBuff.促进) || me.HasStatus(RDMBuff.即刻咏唱))
        {
            if (RedMageHelper.IsInManaActionCombo(me)) return new CheckResult(false, "正在魔三连/焦热决断");
        
            return new CheckResult(true, "打一个");
        }
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(RedMageHelper.GetCurrentSingleWindActionId(Core.Me), ActionType.Gcd, ActionTargetType.Target);
    }
}
