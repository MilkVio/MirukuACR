using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Action.OffGcd;

public class 必杀剑_红莲OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (!PromeSettings.Instance.GetQt(SAMQt.AOE)) return new CheckResult(false, "未开启AOEQT");
        
        var me = Core.Me;
        var kenki = JobGaugeHelper.SAM.剑气;
        var isCanUse = SAMSkill.必杀剑_红莲.GetActionCooldown() == 0 && kenki >= 25 && TargetHelper.EnemyInRange(10) >= 3;
        var hasBoost = me.HasStatus(SAMBuff.风月);
            
        if (!PromeSettings.Instance.GetQt(SAMQt.不打120) && isCanUse)
        {
            if (PromeSettings.Instance.GetQt(SAMQt.倾泻资源))
            {
                return new CheckResult(true, $"倾泻资源");
            }
            
            if (hasBoost)
            {
                return new CheckResult(true, $"有Buff");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SAMSkill.必杀剑_红莲, ActionType.OffGcd, ActionTargetType.Target);
    }
}
