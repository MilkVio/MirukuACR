using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.Gcd;

public class 反转_加速Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        if (Core.Me.Level <82) return new CheckResult(false, $"当前未学会该技能");
        
        var player = Core.Me;
        
        // 自身一定不能用的情况
        if (PctHelper.IsWillMoveOrMoved()) return new CheckResult(false, "当前/未来要移动");
        // QT控制
        // todo
        
        /*
         * 条件 初版
         * 当前有加速法术 优先打反转
         */
        if (player.HasStatus(PCTBuff.绘灵幻景) && player.HasStatus(PCTBuff.减色混合))
        {
            return new CheckResult(true, "团辅内 + 有减色");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return PctHelper.GetBaseGcd(true, false);
    }
}
