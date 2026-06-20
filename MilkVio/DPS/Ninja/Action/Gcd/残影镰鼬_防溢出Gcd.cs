using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.Gcd;

public class 残影镰鼬_防溢出Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(20);
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        var isCanUse = Core.Me.HasStatus(NinjaBuff.残影镰鼬预备);
        if (!isCanUse) return new CheckResult(false, "当前没有预备Buff");
        
        // 理论上这里最好可以打到60/120中
        var statusLeftTime = StatusHelper.GetStatusLeftTime(Core.Me, NinjaBuff.残影镰鼬预备);
        
        if (isCanUse)
        {
            if(statusLeftTime < 2.5f)
            {
                return new CheckResult(true, "防溢出打一个");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(NinjaSkill.残影镰鼬, ActionType.Gcd, ActionTargetType.Target);
    }
}
