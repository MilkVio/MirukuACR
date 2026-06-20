using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;


namespace MilkVio.DPS.Monk.Action.Gcd;

public class 必杀技Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = MonkHelper.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > CurrentMeleeRange) return new CheckResult(false, $"当前目标过远（>{CurrentMeleeRange}m）");
        
        // QT控制
        if (!PromeSettings.Instance.GetQt(MNKQt.必杀技)) return new CheckResult(false, "已关闭必杀技");
        
        if (JobGaugeHelper.MNK.BlitzTimeRemaining > 0)
        {
            return new CheckResult(true, $"距离 <= {{CurrentMeleeRange}}");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(MNKSkill.必杀技, ActionType.Gcd, ActionTargetType.Target);
    }
}
