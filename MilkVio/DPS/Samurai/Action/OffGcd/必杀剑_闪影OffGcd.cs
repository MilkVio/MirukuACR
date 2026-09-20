using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;
using MilkVio.DPS.Samurai.Level100;

namespace MilkVio.DPS.Samurai.Action.OffGcd;

public class 必杀剑_闪影OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (!SamuraiHelper.AllowSenei) return new CheckResult(false, "未开启闪影红莲QT");
        if (Core.Me.Level < 72) return new CheckResult(false, "等级不足");
        if (Samurai100Helper.Enabled && Core.Me.DistanceToMe() > GameData.GetCurrentMeleeRange())
            return new CheckResult(false, "闪影距离不足");
        
        var me = Core.Me;
        var kenki = JobGaugeHelper.SAM.剑气;
        var isCanUse = SAMSkill.必杀剑_闪影.GetActionCooldown() == 0 && kenki >= 25;
        var hasBoost = me.HasStatus(SAMBuff.风月);
        
        if (isCanUse)
        {
            if (Samurai100Helper.Enabled && Samurai100Planning.TryOff(SAMSkill.必杀剑_闪影, out var use, out var reason))
                return new CheckResult(use, reason);
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
        return new PAction(SAMSkill.必杀剑_闪影, ActionType.OffGcd, ActionTargetType.Target);
    }
}
