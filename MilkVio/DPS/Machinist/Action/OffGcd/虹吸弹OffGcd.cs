using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Action.OffGcd;

public class 虹吸弹OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var me = Core.Me;
        if(me.DistanceToMe() > currentAttackRange) return new CheckResult(false, "距离过远");
        
        //if (MachinistHelper.Get虹吸弹CurrentId().GetActionCooldown() < 1f) return new CheckResult(false, "未冷却");
        
        if (MachinistHelper.Get虹吸弹CurrentId().GetActionCharges() < MachinistHelper.Get弹射CurrentId().GetActionCharges())
        {
            return new CheckResult(false, "打另一个");
        }
        
        if (MCHSkill.野火.GetActionCooldown() > 115 && !me.HasStatus(MCHStatus.过热))
            return new CheckResult(false, "团辅防抢过热");
        
        if (MachinistHelper.Get虹吸弹CurrentId().GetActionCharges() >= 1)
        {
            return new CheckResult(true, "所有条件满足 && 冷却完毕");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(MachinistHelper.Get虹吸弹CurrentId(), ActionType.OffGcd, ActionTargetType.Target);
    }
}
