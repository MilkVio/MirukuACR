using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Action.Gcd;

public class 雪月花闪连Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = GameData.GetCurrentMeleeRange();
        var lastComboId = ActionHelper.GetLastComboID();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > CurrentMeleeRange) return new CheckResult(false, $"当前目标过远（>{CurrentMeleeRange}m）");
        var isCanUse = lastComboId == SamuraiHelper.Get1ComboActionId(false) || lastComboId == SAMSkill.阵风 || lastComboId == SAMSkill.士风;
        
        if (Core.Me.DistanceToMe() <= CurrentMeleeRange && isCanUse)
        {
            return new CheckResult(true, $"{CurrentMeleeRange}");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return SamuraiHelper.GetCurrentMsyPAction();
    }
}
