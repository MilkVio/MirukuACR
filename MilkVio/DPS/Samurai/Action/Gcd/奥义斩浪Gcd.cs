using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;
using MilkVio.DPS.Samurai.Level100;

namespace MilkVio.DPS.Samurai.Action.Gcd;

public class 奥义斩浪Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = GameData.GetCurrentAttackRange(8);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > CurrentMeleeRange) return new CheckResult(false, $"当前目标过远（>{CurrentMeleeRange}m）");
        // 回返斩浪不读条。
        if (SamuraiHelper.回返斩浪可用()) return new CheckResult(true, "衔接回返斩浪");
        if (!SamuraiHelper.AllowOgi) return new CheckResult(false, "未开启奥义斩浪QT");
        if (MoveManager.IsLocalPlayerMoving) return new CheckResult(false, $"当前正在移动");
        if (Samurai100Helper.Enabled)
        {
            var use = Samurai100Burst.ShouldUseOgi(out var reason);
            return new CheckResult(use, reason);
        }
        var me = Core.Me;
        var isCanUse = me.HasStatus(SAMBuff.奥义浪斩预备) || SAMSkill.奥义斩浪.GetAdjustedActionId() == SAMSkill.回返斩浪;
        var cd = me.GetStatusLeftTime(SAMBuff.奥义浪斩预备);
        var hasBoost = me.HasStatus(SAMBuff.风月) && me.HasStatus(SAMBuff.风花);
        
        if (Core.Me.DistanceToMe() <= CurrentMeleeRange && isCanUse)
        {
            if (SAMSkill.奥义斩浪.GetAdjustedActionId() == SAMSkill.回返斩浪)
            {
                return new CheckResult(true, $"直接打斩浪");
            }
            
            if (PromeSettings.Instance.GetQt(SAMQt.倾泻资源))
            {
                return new CheckResult(true, $"倾泻资源");
            }
            
            if (cd > 10)
            {
                if (hasBoost)
                {
                    return new CheckResult(true, $"{CurrentMeleeRange} && 有Buff");
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
        if (SamuraiHelper.回返斩浪可用())
        {
            return new PAction(SAMSkill.回返斩浪, ActionType.Gcd, ActionTargetType.Target);
        }
        return new PAction(SAMSkill.奥义斩浪, ActionType.Gcd, ActionTargetType.Target);
    }
}
