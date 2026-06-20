using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.Gcd;

public class 锤子Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        var player = Core.Me;
        
        // QT
        if (PromeSettings.Instance.GetQt(PCTQt.不打锤子)) return new CheckResult(false, "已开启不打锤子");
        
        if (Core.Me.DistanceToMe() <= currentAttackRange && player.HasStatus(PCTBuff.重锤连击))
        {
            if (PCTSkill.风景构想.GetActionCooldown() < 5 && PCTSkill.风景构想.GetActionCooldown() > 0)
            {
                return new CheckResult(true, $"距离 <= {currentAttackRange} && 团辅前瞬发");
            }
            
            if (StatusHelper.GetStatusLeftTime(player, PCTBuff.重锤连击) < 15)
            {
                return new CheckResult(true, $"距离 <= {currentAttackRange} && 要消失了"); 
            }

            if (PromeSettings.Instance.GetQt(PCTQt.快打锤子) || PromeSettings.Instance.GetQt(PCTQt.倾泻资源))
            {
                return new CheckResult(true, $"距离 <= {currentAttackRange} && 资源控制QT开启");
            }
            
            if (PctHelper.IsWillMoveOrMoved())
            {
                return new CheckResult(true, $"距离 <= {currentAttackRange} && 正在移动");
            }

            if (player.HasStatus(PCTBuff.星空构想))
            {
                return new CheckResult(true, $"距离 <= {currentAttackRange} && 在团辅内");
            }
            
            return new CheckResult(false, "有buff时间不够");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return PctHelper.GetHammerGcd();
    }
}
