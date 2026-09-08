using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Reaper.Action.Gcd;

public class 灵魂切割Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ReaperQt.灵魂割)) return new CheckResult(false, "未启用灵魂割");

        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");

        if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, $"当前目标过远（>{currentMeleeRange}m）");
        if (ReaperHelper.IsIn附体() || ReaperHelper.IsIn妖异之镰()) return new CheckResult(false, "当前在附体/妖异之镰");

        if (ReaperHelper.灵魂切层数() < 1) return new CheckResult(false, "灵魂切割没有可用层数");
        if (JobGaugeHelper.RPR.灵魂值 > 50) return new CheckResult(false, "当前灵魂>50 会溢出");

        return new CheckResult(true, "好了就打");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.灵魂切割, ActionType.Gcd, ActionTargetType.Target);
    }
}
