using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;
using MilkVio.DPS.Samurai.Level100;

namespace MilkVio.DPS.Samurai.Action.OffGcd;

public class 意气冲天OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (!SamuraiHelper.AllowIkishoten) return new CheckResult(false, "不打120：暂停意气冲天");
        var isCanUse = SAMSkill.意气冲天.GetActionCooldown() == 0;
        
        if (isCanUse)
        {
            if (Samurai100Helper.Enabled)
            {
                var use = Samurai100Burst.ShouldUseIkishoten(out var reason);
                return new CheckResult(use, reason);
            }
            return new CheckResult(true, $"好了就用");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SAMSkill.意气冲天, ActionType.OffGcd, ActionTargetType.Self);
    }
}
