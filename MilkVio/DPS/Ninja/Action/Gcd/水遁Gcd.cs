using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.Gcd;

public class 水遁Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (!PromeSettings.Instance.GetQt(NinjaQt.水遁)) return new CheckResult(false, "未开启水遁");
        
        var me = Core.Me;
        var hasRenyin = me.HasStatus(NinjaBuff.忍隐);
        var leftTime60 = NinjaHelper.GetNinja60ActionId().GetActionCooldown();
        var ninjyutsuCharge = NinjaHelper.GetCurrentNinjaNinjyutsuCharge();
        
        if (!hasRenyin && !PromeSettings.Instance.GetQt(NinjaQt.不打60) && NinjaHelper.GetCurrentNinjaNinjyutsuCharge() > 1)
        {
            if (leftTime60 < 20f)
            {
                if (ninjyutsuCharge > 1.5)
                {
                    return new CheckResult(true, $"60爆发前快溢出 打一个");
                }
            }
            if (leftTime60 < 5f)
            {
                if (ninjyutsuCharge > 1)
                {
                    return new CheckResult(true, $"刚好 打一个");
                }
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        ActionQueueManager.Enqueue(NinjaHelper.NinjaNinjyutsu.水遁);
        // 不返回任何技能，因为 Group 会自动开始执行
        return null;
    }
}
