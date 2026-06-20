using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.Gcd;

public class 基础反转AOEGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        // 自身一定不能打的情况
        if (PctHelper.IsWillMoveOrMoved()) return new CheckResult(false, "当前/未来要移动");
        
        // QT控制
        if (!PromeSettings.Instance.GetQt(PCTQt.AOE)) return new CheckResult(false, "未开启AOE");
        
        if (Core.Me.DistanceToMe() <= currentAttackRange)
        {
            if (Core.Me.HasStatus(PCTBuff.减色混合))
            {
                if (TargetHelper.EnemyInRangeTarget(Core.Target, 5) >= 3)
                {
                    return new CheckResult(true, $"距离 <= {currentAttackRange} && 目标够多");
                }
            }
            
            return new CheckResult(false, "无buff");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return PctHelper.GetBaseGcd(true, true);
    }
}
