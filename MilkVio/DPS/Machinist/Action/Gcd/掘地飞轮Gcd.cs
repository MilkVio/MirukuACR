using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Action.Gcd;

public class 掘地飞轮Gcd : IDecisionResolver
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
        
        if (me.HasStatus(MCHStatus.掘地飞轮预备))
        {
            return new CheckResult(true, "所有条件满足 && 冷却完毕");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(MCHSkill.掘地飞轮, ActionType.Gcd, ActionTargetType.Target);
    }
}
