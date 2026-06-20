using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Action.Gcd;

public class 祖灵降临Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ViperQt.祖灵降临)) return new CheckResult(false, "未开启QT");
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.Level < 90) return new CheckResult(false, "等级不足");
        
        if (Core.Me.DistanceToMe() > 5) return new CheckResult(false, "当前目标过远（>5m）");
        
        var me = Core.Me;
        
        if (ViperHelper.IsIn附体() || ViperHelper.IsIn强碎灵()) return new CheckResult(false, "当前在附体/强碎灵");

        var isCanUse = ViperHelper.祖灵层数() >= 1;
        
        if (isCanUse)
        {
            // 倾泻资源
            if (PromeSettings.Instance.GetQt(ViperQt.倾泻资源))
            {
                return new CheckResult(true, "倾泻资源 层数>=1");
            }

            // 身上没有buff 先续一个
            if (me.GetStatusLeftTime(ViperBuff.急速) < 10 || me.GetStatusLeftTime(ViperBuff.猛袭) < 10)
            {
                return new CheckResult(false, "先续buff");
            }

            if (!ViperHelper.IsInViper120() && ViperHelper.Is锐牙buff快到期())
            {
                return new CheckResult(false, "先续普通锐牙buff");
            }
            
            var comboLeftTime = ActionHelper.GetComboLeftTime();
            // 续普攻combo
            if (comboLeftTime < 9 && comboLeftTime != 0)
            {
                return new CheckResult(false, "续一下基础连Combo 层数>=1");
            }
            
            // 双附体
            if (ViperHelper.IsInViper120())
            {
                return new CheckResult(true, "双附体 层数>=1");
            }
            
            // 正常逻辑
            if (ViperSkill.蛇灵气.GetActionCooldown() > 40)
            {
                return new CheckResult(true, "120之前正常打一个 层数>=1");
            }
            if (ViperSkill.蛇灵气.GetActionCooldown() <= 40 && PromeSettings.Instance.GetQt(ViperQt.蛇灵气))
            {
                if (JobGaugeHelper.VPR.灵力值 <= 90)
                {
                    return new CheckResult(false, "120之前攒资源双附体 层数>=1");
                }
                return new CheckResult(true, "120之前攒资源 打掉一个 层数>=1");
            }
            
            // 如果没有开启120 理论上这里如果还需要就要按QT控制 所以直接打
            return new CheckResult(true, "正常情况 层数>=1");
        }
            
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(ViperSkill.祖灵降临, ActionType.Gcd, ActionTargetType.Target);
    }
}
