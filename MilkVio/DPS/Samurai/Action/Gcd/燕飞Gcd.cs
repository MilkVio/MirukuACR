using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;
using MilkVio.DPS.Samurai.Level100;

namespace MilkVio.DPS.Samurai.Action.Gcd;

public class 燕飞Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        var currentAttackRange = GameData.GetCurrentAttackRange(20);
        if (!PromeSettings.Instance.GetQt(SAMQt.燕飞)) return new CheckResult(false, "未开启QT");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        if (Samurai100Helper.Enabled && Core.Me.DistanceToMe() > GameData.GetCurrentAttackRange(6))
            return new CheckResult(true, "回返也够不到，使用燕飞");
        
        if (Core.Me.DistanceToMe() > currentMeleeRange && !SamuraiHelper.Has燕回返())
        {
            return new CheckResult(true, $"距离过远");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SAMSkill.燕飞, ActionType.Gcd, ActionTargetType.Target);
    }
}
