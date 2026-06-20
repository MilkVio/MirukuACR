using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.GNB.GNBData;


namespace MilkVio.Tank.GNB.Action.OffGcd;

public class OffGcdTest : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        
        if (Core.Me.DistanceToMe() <= 3f)
        {
            if (GNBSkill.爆破领域.GetActionCooldown() == 0 || GNBSkill.弓形冲波.GetActionCooldown() == 0)
            {
                return new CheckResult(true, "距离 < 3");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if (GNBSkill.爆破领域.GetActionCooldown() == 0)
        {
            return new PAction(GNBSkill.爆破领域, ActionType.OffGcd, ActionTargetType.Target);
        }
        else
        {
            return new PAction(GNBSkill.弓形冲波, ActionType.OffGcd, ActionTargetType.Self);
        }
    }
}
