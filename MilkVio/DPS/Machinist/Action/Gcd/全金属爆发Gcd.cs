using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Action.Gcd;

public class 全金属爆发Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var me = Core.Me;
        if(me.DistanceToMe() > currentAttackRange) return new CheckResult(false, "距离过远");
        var isCantUse = me.HasStatus(MCHStatus.过热);
        if (isCantUse) return new CheckResult(false, "无法使用");
        
        
        if (me.HasStatus(MCHStatus.全金属爆发预备))
        {
            var 野火cd = MCHSkill.野火.GetActionCooldown();
            
            if (StatusHelper.GetStatusLeftTime(me, MCHStatus.全金属爆发预备) < 5)
            {
                return new CheckResult(true, "极限时间");
            }

            if (野火cd >= 100)
            {
                return new CheckResult(true, "野火团辅内");
            }
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(MCHSkill.全金属爆发, ActionType.Gcd, ActionTargetType.Target);
    }
}
