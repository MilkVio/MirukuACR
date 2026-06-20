using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.GNB.GNBData;


namespace MilkVio.Tank.GNB.Action.OffGcd;

public class 烈牙OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        
        if (Core.Me.DistanceToMe() <= 3f)
        {
            if (ActionHelper.GetAdjustedActionId(GNBSkill.续剑) != GNBSkill.续剑)
            {
                return new CheckResult(true, "距离 < 3 同时 存在续剑能力技");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if(ActionHelper.GetAdjustedActionId(GNBSkill.续剑) == GNBSkill.撕喉) return new PAction(GNBSkill.撕喉, ActionType.OffGcd, ActionTargetType.Target);
        if(ActionHelper.GetAdjustedActionId(GNBSkill.续剑) == GNBSkill.裂膛) return new PAction(GNBSkill.裂膛, ActionType.OffGcd, ActionTargetType.Target);
        if(ActionHelper.GetAdjustedActionId(GNBSkill.续剑) == GNBSkill.穿目) return new PAction(GNBSkill.穿目, ActionType.OffGcd, ActionTargetType.Target);
        return new PAction(GNBSkill.超音速, ActionType.OffGcd, ActionTargetType.Target);
    }
}
