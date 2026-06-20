using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Reaper.Action.Gcd;

public class 绞决缢杀Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        var me = Core.Me;
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, $"当前目标过远（>{currentMeleeRange}m）");
        if (ReaperHelper.IsIn附体()) return new CheckResult(false, "当前在附体");

        if (ReaperHelper.IsIn妖异之镰()) return new CheckResult(true, "当前在妖异之镰");
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var me = Core.Me;
        var target = Core.Target;
        var positional = TargetHelper.GetTargetPositional();
        if (ActionHelper.IsActionHighlighted(ReaperSkill.绞决处刑) && ActionHelper.IsActionHighlighted(ReaperSkill.缢杀处刑) && me.HasStatus(ReaperBuff.处刑人Buff) && TargetHelper.HasPositionalRequirement(target))
        {
            if (positional == Positional.Rear)
                return new PAction(ReaperSkill.缢杀处刑, ActionType.Gcd, ActionTargetType.Target);
            if (positional == Positional.Flank)
                return new PAction(ReaperSkill.绞决处刑, ActionType.Gcd, ActionTargetType.Target);
            return new PAction(ReaperSkill.缢杀处刑, ActionType.Gcd, ActionTargetType.Target);
        }
        if(ActionHelper.IsActionHighlighted(ReaperSkill.绞决处刑) && me.HasStatus(ReaperBuff.处刑人Buff)) return new PAction(ReaperSkill.绞决处刑, ActionType.Gcd, ActionTargetType.Target);
        if(ActionHelper.IsActionHighlighted(ReaperSkill.缢杀处刑) && me.HasStatus(ReaperBuff.处刑人Buff)) return new PAction(ReaperSkill.缢杀处刑, ActionType.Gcd, ActionTargetType.Target);
        
        if (me.HasStatus(ReaperBuff.绞决效果提高Buff) && me.HasStatus(ReaperBuff.缢杀效果提高Buff) && TargetHelper.HasPositionalRequirement(target))
        {
            if (positional == Positional.Rear)
                return new PAction(ReaperSkill.缢杀, ActionType.Gcd, ActionTargetType.Target);
            if (positional == Positional.Flank)
                return new PAction(ReaperSkill.绞决, ActionType.Gcd, ActionTargetType.Target);
            return new PAction(ReaperSkill.缢杀, ActionType.Gcd, ActionTargetType.Target);
        }
        if(me.HasStatus(ReaperBuff.绞决效果提高Buff)) return new PAction(ReaperSkill.绞决, ActionType.Gcd, ActionTargetType.Target);
        if(me.HasStatus(ReaperBuff.缢杀效果提高Buff)) return new PAction(ReaperSkill.缢杀, ActionType.Gcd, ActionTargetType.Target);
        return new PAction(ReaperSkill.缢杀, ActionType.Gcd, ActionTargetType.Target);
    }
}
