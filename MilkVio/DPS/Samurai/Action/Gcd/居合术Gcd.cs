using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Action.Gcd;

public class 居合术Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = GameData.GetCurrentAttackRange(6);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > CurrentMeleeRange) return new CheckResult(false, $"当前目标过远（>{CurrentMeleeRange}m）");
        if (MoveManager.IsLocalPlayerMoving) return new CheckResult(false, $"当前正在移动");
        
        if (Core.Me.DistanceToMe() <= CurrentMeleeRange && SamuraiHelper.GetBestJuhe() != 居合类型.无)
        {
            return new CheckResult(true, $"{CurrentMeleeRange}");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SAMSkill.居合术, ActionType.Gcd, ActionTargetType.Target);
    }
}
