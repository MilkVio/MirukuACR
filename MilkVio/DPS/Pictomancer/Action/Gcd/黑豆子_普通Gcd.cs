using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.Gcd;

public class 黑豆子_普通Gcd : IDecisionResolver
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
         * 0.当前/未来移动
         * 1.团辅期内直接打掉？因为加速颜色和那几个buff都是高优先级的
         * 2.QT倾泻资源
         * 3.正常情况
         */
        if (Core.Me.DistanceToMe() <= currentAttackRange && canUse)
        {
            if (PctHelper.IsWillMoveOrMoved()) return new CheckResult(true, "当前/未来要移动");
            
            if (player.HasStatus(PCTBuff.星空构想))
            {
                return new CheckResult(true, $"在团辅中"); 
            }
            
            if (MoveManager.IsLocalPlayerMoving)
            {
                return new CheckResult(true, $"距离 <= {currentAttackRange} && 正在移动");
            }
            
            if (PromeSettings.Instance.GetQt(PCTQt.倾泻资源))
            {
                return new CheckResult(true, $"距离 <= {currentAttackRange} && 正在倾泻资源");
            }

            if (!player.HasStatus(PCTBuff.减色混合))
            {
                return new CheckResult(true, $"正常情况打一个"); 
            }
            
            return new CheckResult(false, "有buff时间不够");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.彗星之黑, ActionType.Gcd, ActionTargetType.Target);
    }
}
