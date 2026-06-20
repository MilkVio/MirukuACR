using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Action.Gcd;

public class 基础连击Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, $"当前目标过远（>{currentMeleeRange}m）");
        if (ViperHelper.IsIn附体() || ViperHelper.IsIn强碎灵()) return new CheckResult(false, "当前在附体/强碎灵");
        
        return new CheckResult(true, "基础连击");
    }

    public PAction GetAction()
    {
        var me = Core.Me;
        var ap = ActionPhase();
        if (ap == ViperActionPhase.Phase1)
        {
            if (me.HasStatus(ViperBuff.咬噬锐牙左12))
            {
                return new PAction(ViperSkill.咬噬尖齿左1, ActionType.Gcd, ActionTargetType.Target);
            }
            return new PAction(ViperSkill.穿裂尖齿右1, ActionType.Gcd, ActionTargetType.Target);
        }

        if (ap == ViperActionPhase.Phase2)
        {
            if (ViperSkill.猛袭利齿左2.IsActionHighlighted() && ViperSkill.急速利齿右2.IsActionHighlighted())
            {
                if (!me.HasStatus(ViperBuff.急速))
                    return new PAction(ViperSkill.急速利齿右2, ActionType.Gcd, ActionTargetType.Target);
                return new PAction(ViperSkill.猛袭利齿左2, ActionType.Gcd, ActionTargetType.Target);
            }

            if (ViperSkill.猛袭利齿左2.IsActionHighlighted())
            {
                return new PAction(ViperSkill.猛袭利齿左2, ActionType.Gcd, ActionTargetType.Target);
            }
            
            return new PAction(ViperSkill.急速利齿右2, ActionType.Gcd, ActionTargetType.Target);
        }

        if (ap == ViperActionPhase.Phase3绿)
        {
            // 绿增益
            if (me.HasStatus(ViperBuff.侧击锐牙绿左3))
                return new PAction(ViperSkill.侧击獠齿左3绿, ActionType.Gcd, ActionTargetType.Target);
            return new PAction(ViperSkill.侧裂獠齿右3绿, ActionType.Gcd, ActionTargetType.Target);
        }
        
        if (ap == ViperActionPhase.Phase3红)
        {
            // 红增益
            if (me.HasStatus(ViperBuff.背击锐牙红左3))
                return new PAction(ViperSkill.侧击獠齿左3红, ActionType.Gcd, ActionTargetType.Target);
            return new PAction(ViperSkill.侧裂獠齿右3红, ActionType.Gcd, ActionTargetType.Target);
        }
        
        return new PAction(ViperSkill.穿裂尖齿右1, ActionType.Gcd, ActionTargetType.Target);
    }

    private ViperActionPhase ActionPhase()
    {
        switch (ViperSkill.穿裂尖齿右1.GetAdjustedActionId())
        {
            case ViperSkill.急速利齿右2:
                return ViperActionPhase.Phase2;

            case ViperSkill.侧裂獠齿右3绿:
                return ViperActionPhase.Phase3绿;
            
            case ViperSkill.侧裂獠齿右3红:
                return ViperActionPhase.Phase3红;

            default:
                return ViperActionPhase.Phase1;
        }
    }
    
    private enum ViperActionPhase
    {
        Phase1 = 1,
        Phase2 = 2,
        Phase3绿 = 3,
        Phase3红 = 4
    }
}
