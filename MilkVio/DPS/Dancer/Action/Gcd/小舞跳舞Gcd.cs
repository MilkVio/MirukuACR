using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;
namespace MilkVio.DPS.Dancer.Action.Gcd;

public class 小舞跳舞Gcd : IDecisionResolver

{
    public CheckResult Check()
    {
        var player = Core.Me;
        var nextDanceActionId = JobGaugeHelper.DNC.NextStep;
        
        if (player == null) return new CheckResult(false, "我不存在");
        if (nextDanceActionId == 15998 || nextDanceActionId == 0)return new CheckResult(false, "跳完了");
        if (JobGaugeHelper.DNC.IsDancing && player.HasStatus(DNCBuff.标准舞步)) return new CheckResult(true, "都通过了");
        return new CheckResult(false, "都没通过");
    }

    public PAction GetAction()
    {
        // 当 nextdanceactionid == 15998 || 0的时候 代表 舞步结束 使用释放
        var nextDanceActionId = JobGaugeHelper.DNC.NextStep;
        return new PAction(nextDanceActionId, ActionType.Gcd, ActionTargetType.Self);
    }
}
