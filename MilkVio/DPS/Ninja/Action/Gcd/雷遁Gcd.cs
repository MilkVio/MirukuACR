using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.Gcd;

public class 雷遁Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        var me = Core.Me;
        var isIn60 = NinjaHelper.Is60();
        var isIn120 = NinjaHelper.Is120();
        var cdFor60 = NinjaHelper.GetNinja60ActionId().GetActionCooldown();
        var cdFor120 = NinjaSkill.介毒之术.GetActionCooldown();
        var njtCharge = NinjaHelper.GetCurrentNinjaNinjyutsuCharge();
        
        if (njtCharge >= 1f)
        {
            // 这里分为两种情况？三种？
            // 60秒的时候
            // 120秒的时候
            // 倾泻资源的时候
            if (isIn60 && cdFor120 > 20)
            {
                return new CheckResult(true, "当前为60秒爆发");
            }
            
            if (isIn120)
            {
                if (isIn60)
                {
                    return new CheckResult(true, "当前为双120秒爆发");
                }
                if (cdFor60 < 5)
                {
                    return new CheckResult(false, "留资源双爆发");
                }
                if (cdFor60 > 20 && cdFor60 < 40)
                {
                    return new CheckResult(true, "不太纯粹的120");
                }
            }

            if (PromeSettings.Instance.GetQt(NinjaQt.倾泻资源))
            {
                return new CheckResult(true, "倾泻资源");
            }

            if (PromeSettings.Instance.GetQt(NinjaQt.忍术不溢出))
            {
                if (cdFor60 > 30 && njtCharge > 1.98f) return new CheckResult(true, "忍术不溢出");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        ActionQueueManager.Enqueue(NinjaHelper.NinjaNinjyutsu.雷遁);
        // 不返回任何技能，因为 Group 会自动开始执行
        return null;
    }
}
