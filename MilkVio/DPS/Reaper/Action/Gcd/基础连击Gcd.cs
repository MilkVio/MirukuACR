using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Reaper.Action.Gcd;

public class 基础连击Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, $"当前目标过远（>{currentMeleeRange}m）");
        if (ReaperHelper.IsIn附体() || ReaperHelper.IsIn妖异之镰()) return new CheckResult(false, "当前在附体/妖异之镰");
        
        return new CheckResult(true, "基础连击");
    }

    public PAction GetAction()
    {
        if (ActionHelper.IsActionHighlighted(ReaperSkill.增盈切割))
            return new PAction(ReaperSkill.增盈切割, ActionType.Gcd, ActionTargetType.Target);
        if (ActionHelper.IsActionHighlighted(ReaperSkill.地狱切割))
            return new PAction(ReaperSkill.地狱切割, ActionType.Gcd, ActionTargetType.Target);
        return new PAction(ReaperSkill.切割, ActionType.Gcd, ActionTargetType.Target);
    }
}
