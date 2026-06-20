using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Action.OffGcd;

public class 意气冲天OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var isCanUse = SAMSkill.意气冲天.GetActionCooldown() == 0;
        
        if (!PromeSettings.Instance.GetQt(SAMQt.不打120) && isCanUse)
        {
            return new CheckResult(true, $"好了就用");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SAMSkill.意气冲天, ActionType.OffGcd, ActionTargetType.Self);
    }
}
