using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.Gcd;

public class 释放忍术Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttactRange = GameData.GetCurrentAttackRange(20);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentAttactRange) return new CheckResult(false, $"当前目标过远（>{currentAttactRange}m）");
        var isCanUse = ActionHelper.GetAdjustedActionId(NinjaSkill.忍术) != NinjaSkill.忍术 && Core.Me.HasStatus(NinjaBuff.结印);

        if (isCanUse) return new CheckResult(true, $"距离 <= {currentAttactRange}");

        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(NinjaSkill.忍术, ActionType.Gcd, ActionTargetType.Target);
    }
}
