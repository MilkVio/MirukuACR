using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.Gcd;

public class DotGcd : IDecisionResolver
{
    // 与规划器共用维护窗口；标准爆发仍由自身安排填充。
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ReaperQt.Dot)) return new CheckResult(false, "未开启Dot");
        if (ReaperHelper.IsIn妖异之镰()) return new CheckResult(false, "先消费妖异之镰/处刑");

        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");

        if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, $"当前目标过远（>{currentMeleeRange}m）");

        var planner = ReaperBattleData.Instance.Planner;
        if (planner.Current.Enshrouded > 0 && !planner.Current.IsDump)
            return new CheckResult(ReaperResources.NeedsDesignInShroud(planner.Current), "附体必要续印");
        if (planner.Current.IsDump ? planner.GcdAction == ReaperSkill.死亡之影
            : ReaperResources.ShouldRefreshDeathDesign(planner.Current, !planner.IsPlanned))
            return new CheckResult(true, "续死亡烙印");

        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.死亡之影, ActionType.Gcd, ActionTargetType.Target);
    }
}
