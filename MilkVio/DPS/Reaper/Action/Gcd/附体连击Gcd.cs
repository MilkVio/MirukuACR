using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.Gcd;

public class 附体连击Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!ReaperHelper.IsIn附体()) return new CheckResult(false, "当前不在附体");

        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        if (JobGaugeHelper.RPR.夜游魂 == 0) return new CheckResult(false, "没有夜游魂");

        if (JobGaugeHelper.RPR.夜游魂 > 1 || !ActionHelper.IsActionAvailableByLevelAndQuest(ReaperSkill.团契))
        {
            var currentMeleeRange = GameData.GetCurrentMeleeRange();
            if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, $"当前目标过远（>{currentMeleeRange}m）");
            return new CheckResult(true, "虚无收割/交错收割");
        }

        // 夜游魂 == 1 只能打团契 读条技能 移动中不打
        if (Core.Me.DistanceToMe() > GameData.GetCurrentAttackRange(25)) return new CheckResult(false, "团契目标过远");
        if (MoveManager.IsLocalPlayerMoving) return new CheckResult(false, "团契需要读条 当前正在移动");
        return new CheckResult(true, "团契");
    }

    public PAction GetAction()
    {
        if (JobGaugeHelper.RPR.夜游魂 > 1 || !ActionHelper.IsActionAvailableByLevelAndQuest(ReaperSkill.团契))
        {
            if (ActionHelper.IsActionHighlighted(ReaperSkill.交错收割))
                return new PAction(ReaperSkill.交错收割, ActionType.Gcd, ActionTargetType.Target);
            return new PAction(ReaperSkill.虚无收割, ActionType.Gcd, ActionTargetType.Target);
        }
        return new PAction(ReaperSkill.团契, ActionType.Gcd, ActionTargetType.Target);
    }
}
