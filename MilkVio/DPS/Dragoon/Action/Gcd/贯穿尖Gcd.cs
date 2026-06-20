using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;


namespace MilkVio.DPS.Dragoon.Action.Gcd;

public class 贯穿尖Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = GameData.GetCurrentMeleeRange();
        var CurrentAttackRange = GameData.GetCurrentAttackRange(20);
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > CurrentAttackRange) return new CheckResult(false, $"当前目标过远（>{CurrentMeleeRange}m）");
        
        // 自身不可以的条件
        if (!PromeSettings.Instance.GetQt(DRGQt.贯穿尖)) return new CheckResult(false, "未开启贯穿尖");
        
        if (Core.Me.DistanceToMe() > CurrentMeleeRange)
        {
            return new CheckResult(true, $"远离目标");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(DRGSkill.贯穿尖, ActionType.Gcd, ActionTargetType.Target);
    }
}
