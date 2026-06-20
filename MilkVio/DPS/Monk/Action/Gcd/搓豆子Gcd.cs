using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;


namespace MilkVio.DPS.Monk.Action.Gcd;

public class 搓豆子Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = MonkHelper.GetCurrentMeleeRange();
        if (Core.Target != null)
        {
            if (Core.Me.DistanceToMe() < CurrentMeleeRange) return new CheckResult(false, $"当前目标过近（<{CurrentMeleeRange}m）");
        }
        
        
        if (JobGaugeHelper.MNK.Chakra < 5)
        {
            return new CheckResult(true, $"距离 > {CurrentMeleeRange}m && 豆子不足");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(MNKSkill.搓豆子, ActionType.Gcd, ActionTargetType.Self);
    }
}
