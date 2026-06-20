using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.Gcd;

public class 黑豆子_加速Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        if (Core.Me.Level < 90) return new CheckResult(false, $"当前未学会该技能");
        
        var player = Core.Me;
        var canUse = JobGaugeHelper.PCT.Paint > 0 && player.HasStatus(PCTBuff.色调反转);
        
        // QT控制
        // todo
        
        /*
         * 条件 初版
         * 当前有加速法术 次打黑豆子
         */
        if (Core.Me.DistanceToMe() <= currentAttackRange && canUse)
        {
            if (player.HasStatus(PCTBuff.绘灵幻景))
            {
                return new CheckResult(true, $"在加速中"); 
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.彗星之黑, ActionType.Gcd, ActionTargetType.Target);
    }
}
