using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Action.OffGcd;

public class 残心OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var me = Core.Me;
        var kenki = JobGaugeHelper.SAM.剑气;
        var isCanUse = SAMSkill.残心.GetActionCooldown() == 0 && kenki >= 50 && Core.Me.HasStatus(SAMBuff.残心预备);
        var cd = me.GetStatusLeftTime(SAMBuff.残心预备);
        var hasBoost = me.HasStatus(SAMBuff.风月) && me.HasStatus(SAMBuff.风花);
        
        if (!PromeSettings.Instance.GetQt(SAMQt.不打120) && isCanUse)
        {
            if (PromeSettings.Instance.GetQt(SAMQt.倾泻资源))
            {
                return new CheckResult(true, $"倾泻资源");
            }
            
            if (cd > 10)
            {
                if (hasBoost)
                {
                    return new CheckResult(true, $"有Buff");
                }
                return new CheckResult(false, "当前无增益buff");
            }

            if (cd <= 10)
            {
                return new CheckResult(true, $"无buff但是快过期了");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SAMSkill.残心, ActionType.OffGcd, ActionTargetType.Target);
    }
}
