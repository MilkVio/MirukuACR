using System.Collections.Generic;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Action.OffGcd;

public class 整备回转飞锯OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var me = Core.Me;
        if(me.DistanceToMe() > currentAttackRange) return new CheckResult(false, "距离过远");
        var isCantUse = me.HasStatus(MCHStatus.整备) || me.HasStatus(MCHStatus.过热);
        if (isCantUse) return new CheckResult(false, "无法使用");
        if (me.Level < 90) return new CheckResult(false, "等级不足");
        
        if (ActionHelper.GetGcdRemain() > 1.5) return new CheckResult(false, "不在GCD后半段");
        
        if (MCHSkill.整备.GetActionCharges() >= 1 && MCHSkill.回转飞锯.GetActionCooldown() < 1)
        {
            return new CheckResult(true, "所有条件满足 && 冷却完毕");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        List<PAction> 整备回转飞锯 = new List<PAction>
        {
            new(MCHSkill.整备, ActionType.OffGcd, ActionTargetType.Self),
            new(MCHSkill.回转飞锯, ActionType.Gcd, ActionTargetType.Target),
        };
        ActionQueueManager.EnqueueOffGcdList(整备回转飞锯);
        // 不返回任何技能，因为 Group 会自动开始执行
        return null;
    }
}
