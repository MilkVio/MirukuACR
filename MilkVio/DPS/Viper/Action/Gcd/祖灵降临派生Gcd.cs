using Dalamud.Game.ClientState.JobGauge.Enums;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Action.Gcd;

public class 祖灵降临派生Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, $"当前目标过远（>{currentMeleeRange}m）");

        if (ViperHelper.IsIn附体()) return new CheckResult(true, "当前在附体");
            
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        if(ViperSkill.祖灵之牙一式.IsActionHighlighted()) return new PAction(ViperSkill.祖灵之牙一式, ActionType.Gcd, ActionTargetType.Target);
        if(ViperSkill.祖灵之牙二式.IsActionHighlighted()) return new PAction(ViperSkill.祖灵之牙二式, ActionType.Gcd, ActionTargetType.Target);
        if(ViperSkill.祖灵之牙三式.IsActionHighlighted()) return new PAction(ViperSkill.祖灵之牙三式, ActionType.Gcd, ActionTargetType.Target);
        if(ViperSkill.祖灵之牙四式.IsActionHighlighted()) return new PAction(ViperSkill.祖灵之牙四式, ActionType.Gcd, ActionTargetType.Target);
        return new PAction(ViperSkill.祖灵降临.GetAdjustedActionId(), ActionType.Gcd, ActionTargetType.Target);
    }
}
