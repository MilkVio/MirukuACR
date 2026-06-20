using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;
namespace MilkVio.DPS.Dancer.Action.Gcd;

public class 小舞结束Gcd : IDecisionResolver

{
    public CheckResult Check()
    {
        var player = Core.Me;
        var nextDanceActionId = JobGaugeHelper.DNC.NextStep;
        
        if (player == null) return new CheckResult(false, "我不存在");
        if (PromeSettings.Instance.GetQt(DNCQt.强制小舞和打出))
        {
            if (JobGaugeHelper.DNC.IsDancing && player.HasStatus(DNCBuff.标准舞步))
            {
                if (nextDanceActionId == 15998 || nextDanceActionId == 0) return new CheckResult(true, "强制打小舞");
            }
        }
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        //跳舞状态 大
        if (JobGaugeHelper.DNC.IsDancing && player.HasStatus(DNCBuff.标准舞步))
        {
            if (nextDanceActionId == 15998 || nextDanceActionId == 0)return new CheckResult(true, "都通过了");
        }
        
        return new CheckResult(false, "没跳完舞");
    }

    public PAction GetAction()
    {
        if (PromeSettings.Instance.GetQt(DNCQt.强制小舞和打出))return new PAction(DNCSkill.标准舞步结束,ActionType.Gcd, ActionTargetType.Self);
        return new PAction(DNCSkill.标准舞步结束,ActionType.Gcd, ActionTargetType.Target);
    }
}
