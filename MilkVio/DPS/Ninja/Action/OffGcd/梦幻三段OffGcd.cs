using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.OffGcd;

public class 梦幻三段OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(3);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        var isCanUse = NinjaSkill.梦幻三段.GetActionCooldown();
        
        // QT控制
        
        if (isCanUse == 0)
        {
            if (!PromeSettings.Instance.GetQt(NinjaQt.不打60))
            {
                return new CheckResult(true, "打一个");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        return new PAction(NinjaSkill.梦幻三段, ActionType.OffGcd, ActionTargetType.Target);
    }
}
