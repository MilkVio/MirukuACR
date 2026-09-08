using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.OffGcd;

public class 隐匿挥割OffGcd : IDecisionResolver
{
    // 打完之后会进入绞决缢杀Gcd状态 带身位优化
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ReaperQt.隐匿挥割)) return new CheckResult(false, "未开启隐匿挥割");
        if (JobGaugeHelper.RPR.灵魂值 < 50) return new CheckResult(false, "灵魂值不足50");
        if (ReaperHelper.IsIn附体() || ReaperHelper.IsIn妖异之镰()) return new CheckResult(false, "当前在附体/妖异之镰");

        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");

        if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, $"当前目标过远（>{currentMeleeRange}m）");
        if (ReaperHelper.需要续连击(ReaperHelper.普通Gcd)) return new CheckResult(false, "隐匿挥割前先续连击");

        // 身位由下一招绞决/缢杀选择；不为了等身位长期阻止灵魂消费。
        return new CheckResult(true, "按资源安排消费灵魂");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.隐匿挥割.GetAdjustedActionId(), ActionType.OffGcd, ActionTargetType.Target);
    }
}
