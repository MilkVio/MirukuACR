using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.Gcd;

public class 宝石耀Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        // 自身不可以的条件
        if (JobGaugeHelper.SMN.IsIfritAttuned)
        {
            if (MoveManager.IsLocalPlayerMoving && !me.HasStatus(SMNBuff.即刻咏唱)) return new CheckResult(false, "为火神且当前正在移动");
        }

        if (JobGaugeHelper.SMN.AttunementCount == 0) return new CheckResult(false, "打没了");
        
        // 决策不允许的条件
        if (SummonerHelper.IsBahamutAttuned()) return new CheckResult(false, "当前正在龙神召唤");
        
        if (PromeSettings.Instance.GetQt(SMNQt.龙神召唤))
        {
            if (SMNSkill.龙神召唤.GetActionCooldown() <= 1) return new CheckResult(false, "当前有龙神召唤");
        }
        
        return new CheckResult(true, "打一个");
    }

    public PAction GetAction()
    {
        
        return new PAction(SMNSkill.宝石耀, ActionType.Gcd, ActionTargetType.Target);
    }
}
