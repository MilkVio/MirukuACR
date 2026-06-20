using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.Gcd;

public class 冰晶Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        var me = Core.Me;
        var hasShengsha = me.HasStatus(NinjaBuff.生杀);
        var leftTime60 = NinjaHelper.GetNinja60ActionId().GetActionCooldown();
        var leftTime4Shengsha = Core.Me.GetStatusLeftTime(NinjaBuff.生杀);
        var leftTime = leftTime4Shengsha - leftTime60;
        var isIn60 = NinjaHelper.Is60();

        if (hasShengsha && PromeSettings.Instance.GetQt(NinjaQt.倾泻资源)) return new CheckResult(true, "倾泻资源");
            
        if (hasShengsha && !PromeSettings.Instance.GetQt(NinjaQt.不打60))
        {
            if (isIn60) return new CheckResult(true, "当前正在60");
            if (leftTime < 5) return new CheckResult(false, "当前等待一下");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        ActionQueueManager.Enqueue(NinjaHelper.NinjaNinjyutsu.冰晶);
        // 不返回任何技能，因为 Group 会自动开始执行
        return null;
    }
}
