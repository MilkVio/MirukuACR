using Dalamud.Game.ClientState.JobGauge.Enums;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Action.Gcd;

public class 强碎灵蛇派生Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, $"当前目标过远（>{currentMeleeRange}m）");

        if (ViperHelper.IsIn强碎灵()) return new CheckResult(true, "当前在强碎灵");
            
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var me = Core.Me;
        
        // 优先续buff 身位不匹配交给真北/TP身位
        if (me.GetStatusLeftTime(ViperBuff.急速) < 5) return new PAction(ViperSkill.急速盘蛇, ActionType.Gcd, ActionTargetType.Target);
        if (me.GetStatusLeftTime(ViperBuff.猛袭) < 5) return new PAction(ViperSkill.猛袭盘蛇, ActionType.Gcd, ActionTargetType.Target);
        
        // 如果有两层 优先身位控制
        if (JobGaugeHelper.VPR.蛇剑连状态 == DreadCombo.Dreadwinder)
        {
            var positional = TargetHelper.GetTargetPositional();
            // 侧面
            if (positional == Positional.Flank)
                return new PAction(ViperSkill.猛袭盘蛇, ActionType.Gcd, ActionTargetType.Target);
            if (positional == Positional.Rear)
                return new PAction(ViperSkill.急速盘蛇, ActionType.Gcd, ActionTargetType.Target);
        }

        if (JobGaugeHelper.VPR.蛇剑连状态 == DreadCombo.SwiftskinsCoil)
            return new PAction(ViperSkill.猛袭盘蛇, ActionType.Gcd, ActionTargetType.Target);
        
        return new PAction(ViperSkill.急速盘蛇, ActionType.Gcd, ActionTargetType.Target);
    }
}
