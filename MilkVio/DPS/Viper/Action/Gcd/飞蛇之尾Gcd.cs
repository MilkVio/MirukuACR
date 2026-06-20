using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Action.Gcd;

public class 飞蛇之尾Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ViperQt.飞蛇之尾)) return new CheckResult(false, "未开启QT");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        if (Core.Me.Level < 82) return new CheckResult(false, "等级不足");
        
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        var currentAttackRange = GameData.GetCurrentAttackRange(20);
        var stacks = JobGaugeHelper.VPR.飞蛇之魂层数;
        var isCanUse = stacks >= 1;
        var me = Core.Me;
        
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");

        if (isCanUse)
        {
            // 倾泻资源
            if (PromeSettings.Instance.GetQt(ViperQt.倾泻资源) && Core.Me.Level >= 92)
            {
                if (ViperHelper.IsIn附体() || ViperHelper.IsIn强碎灵()) return new CheckResult(false, "当前在附体/强碎灵");
                return new CheckResult(true, "倾泻资源 层数>=1");
            }
            
            // 身上没有buff 先续一个
            if (me.GetStatusLeftTime(ViperBuff.急速) < 10 || me.GetStatusLeftTime(ViperBuff.猛袭) < 10 || ViperHelper.Is锐牙buff快到期())
            {
                return new CheckResult(false, "先续buff");
            }
            
            // 90级学会徐剑之前这个似乎不赚
            if (PromeSettings.Instance.GetQt(ViperQt.倾泻资源) && Core.Me.Level < 92)
            {
                if (ViperHelper.强碎灵层数() < 1) return new CheckResult(true, "倾泻资源 层数>=1");
            }
                
            if (Core.Target.DistanceToMe() > currentMeleeRange)
            {
                return new CheckResult(true, "当前目标过远 层数>=1");
            }
            
            var comboLeftTime = ActionHelper.GetComboLeftTime();
            // 续普攻combo
            if (comboLeftTime < 9 && comboLeftTime != 0)
            {
                return new CheckResult(false, "续一下基础连Combo 层数>=1");
            }
            
            if (ViperHelper.IsIn附体() || ViperHelper.IsIn强碎灵()) return new CheckResult(false, "当前在附体/强碎灵");
            
            // 100级 在120中
            if(ViperHelper.IsInViper120() && Core.Me.Level >= 92)
            {
                return new CheckResult(true, "120双附体 层数>=1");
            }
            
            // 正常防溢出
            if (stacks == 3) return new CheckResult(true, "层数=3 防溢出");

            if (ViperSkill.蛇灵气.GetActionCooldown() <= 40 && PromeSettings.Instance.GetQt(ViperQt.蛇灵气))
            {
                if (JobGaugeHelper.VPR.灵力值 >= 50 && stacks >= 2) 
                {
                    return new CheckResult(true, "爆发前只留一个 防溢出");
                }
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(ViperSkill.飞蛇之尾, ActionType.Gcd, ActionTargetType.Target);
    }
}
