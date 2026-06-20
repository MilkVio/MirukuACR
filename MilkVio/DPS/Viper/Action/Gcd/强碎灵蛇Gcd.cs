using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Action.Gcd;

public class 强碎灵蛇Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ViperQt.强碎灵蛇)) return new CheckResult(false, "未开启QT");
        
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, $"当前目标过远（>{currentMeleeRange}m）");

        if (ViperHelper.IsIn附体() || ViperHelper.IsIn强碎灵()) return new CheckResult(false, "当前在附体/强碎灵");
        
        if (ViperHelper.强碎灵层数() >= 1)
        {
            // 倾泻资源 
            if (PromeSettings.Instance.GetQt(ViperQt.倾泻资源))
            {
                return new CheckResult(true, "倾泻资源 层数>=1");
            }

            var comboLeftTime = ActionHelper.GetComboLeftTime();
            // 续普攻combo
            if (comboLeftTime < 9 && comboLeftTime != 0)
            {
                return new CheckResult(false, "续一下基础连Combo 层数>=1");
            }
            
            if (!ViperHelper.IsInViper120() && ViperHelper.Is锐牙buff快到期())
            {
                return new CheckResult(false, "先续普通锐牙buff");
            }
            
            if (Core.Me.Level >= 86)
            {
                // 120之前不开
                if (((ViperSkill.蛇灵气.GetActionCooldown() < 5 || ViperSkill.蛇灵气.GetActionCooldown() > 115) && EngageManager.GetBattleTime() > 5) &&
                    PromeSettings.Instance.GetQt(ViperQt.蛇灵气))
                {
                    return new CheckResult(false ,"准备120双附体 层数>=1");
                }
            }
            
            return new CheckResult(true, "层数>=1");
        }
            
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(ViperSkill.强碎灵蛇, ActionType.Gcd, ActionTargetType.Target);
    }
}
