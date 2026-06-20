using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.Gcd;

public class 宝石兽召唤Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        // 自身不可以的条件
        if (MoveManager.IsLocalPlayerMoving) return new CheckResult(false, "当前正在移动"); 
        
        // 决策不允许的条件
        if (!JobGaugeHelper.SMN.HasPet && !SummonerHelper.IsBahamutAttuned() && !JobGaugeHelper.SMN.IsGarudaReady && !JobGaugeHelper.SMN.IsIfritReady && !JobGaugeHelper.SMN.IsTitanReady) return new CheckResult(true, "召唤一下宝石兽");
        
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SMNSkill.宝石兽召唤, ActionType.Gcd, ActionTargetType.Self);
    }
}
