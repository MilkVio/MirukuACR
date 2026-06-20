using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.RedMage.RDMData;

namespace MilkVio.DPS.RedMage.Action.Gcd;

public class 赤雷_单体Gcd: IDecisionResolver
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
        if (!RedMageHelper.HasContinuousCast(me)) return new CheckResult(false, "当前没有连续咏唱");
        
        // 决策不可以的条件
        var fireStatusLeftTime = StatusHelper.GetStatusLeftTime(me, RDMBuff.赤火炎预备);
        var stoneStatusLeftTime = StatusHelper.GetStatusLeftTime(me, RDMBuff.赤飞石预备);
        if (RedMageHelper.IsInManaActionCombo(me)) return new CheckResult(false, "正在魔三连/焦热决断");
        
        if (JobGaugeHelper.RDM.BlackMana - JobGaugeHelper.RDM.WhiteMana + 6 >= 30)
        {
            return new CheckResult(false, "别他妈打黑了 快失衡了");
        }

        if (RedMageHelper.HasRedFireReady(me) && !RedMageHelper.HasRedStoneReady(me))
        {
            return new CheckResult(false, "触发一个赤飞石");
        }
        
        if (RedMageHelper.GetLowestManaType() == ManaType.WhiteMana)
        {
            return new CheckResult(false, "平衡魔元打白");
        }
        
        return new CheckResult(true, "打一个");
    }

    public PAction GetAction()
    {
        return new PAction(RedMageHelper.GetCurrentSingleThunderActionId(Core.Me), ActionType.Gcd, ActionTargetType.Target);
    }
}
