using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.OffGcd;

public class 暴食OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ReaperQt.暴食)) return new CheckResult(false, "未开启暴食");
        if (JobGaugeHelper.RPR.灵魂值 < 50) return new CheckResult(false, "灵魂值不足50");
        if (ReaperHelper.IsIn附体() || ReaperHelper.IsIn妖异之镰()) return new CheckResult(false, "当前在附体/妖异之镰");
        if (ReaperSkill.暴食.GetActionCooldown() > 0.5f) return new CheckResult(false, "暴食未冷却");

        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");

        if (Core.Me.DistanceToMe() > GameData.GetCurrentAttackRange(25)) return new CheckResult(false, "当前目标过远（>25m）");
        if (ReaperHelper.需要续连击(2 * ReaperHelper.普通Gcd)) return new CheckResult(false, "暴食前先续连击");

        return new CheckResult(true, "暴食好了就打");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.暴食, ActionType.OffGcd, ActionTargetType.Target);
    }
}
