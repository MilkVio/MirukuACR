using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;
namespace MilkVio.DPS.Dancer.Action.Gcd;

public class 大舞结束Gcd : IDecisionResolver

{
    public CheckResult Check()
    {
        var player = Core.Me;
        var nextDanceActionId = JobGaugeHelper.DNC.NextStep;
        
        
        if (player == null) return new CheckResult(false, "我不存在");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        //跳舞状态 大
        if (JobGaugeHelper.DNC.IsDancing && player.HasStatus(DNCBuff.技巧舞步))
        {
            if (nextDanceActionId == 15998 || nextDanceActionId == 0)return new CheckResult(true, "都通过了");
        }
        return new CheckResult(false, "没跳完舞");
    }

    public PAction GetAction()
    {
        return new PAction(DNCSkill.技巧舞步结束,ActionType.Gcd, ActionTargetType.Target);
    }
}
