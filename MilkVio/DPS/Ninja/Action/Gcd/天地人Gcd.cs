using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.Gcd;

public class 天地人Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        var isCanUse = Core.Me.HasStatus(NinjaBuff.天地人);
        
        if (Core.Me.DistanceToMe() <= currentAttackRange && isCanUse)
        {
            return new CheckResult(true, $"距离 <= {currentAttackRange}");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        ActionQueueManager.Enqueue(NinjaHelper.NinjaNinjyutsu.天地人单体);
        // 不返回任何技能，因为 Group 会自动开始执行
        return null;
    }
}
