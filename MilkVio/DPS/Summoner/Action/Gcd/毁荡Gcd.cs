using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.Gcd;

public class 毁荡Gcd : IDecisionResolver
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
        if (MoveManager.IsLocalPlayerMoving) return new CheckResult(false, "当前正在移动");
        
        // 决策不允许的条件
        if (!JobGaugeHelper.SMN.HasPet) return new CheckResult(false, "未召唤宝石兽");
        
        if (!PromeSettings.Instance.GetQt(SMNQt.不打三神))
        {
            if (JobGaugeHelper.SMN.IsTitanReady) return new CheckResult(false, "当前有土神召唤");
            if (JobGaugeHelper.SMN.IsGarudaReady) return new CheckResult(false, "当前有风神召唤");
            if (JobGaugeHelper.SMN.IsIfritReady) return new CheckResult(false, "当前有火神召唤");
        }
        
        if (SummonerHelper.IsBahamutAttuned()) return new CheckResult(false, "当前正在龙神召唤");
        if (JobGaugeHelper.SMN.IsTitanAttuned) return new CheckResult(false, "当前正在土神召唤");
        if (JobGaugeHelper.SMN.IsGarudaAttuned) return new CheckResult(false, "当前正在风神召唤");
        if (JobGaugeHelper.SMN.IsIfritAttuned) return new CheckResult(false, "当前正在火神召唤");
        
        if (PromeSettings.Instance.GetQt(SMNQt.龙神召唤))
        {
            if (SMNSkill.龙神召唤.GetActionCooldown() <= 1) return new CheckResult(false, "当前有龙神召唤");
        }
        
        return new CheckResult(true, "打一个");
    }

    public PAction GetAction()
    {
        
        return new PAction(SMNSkill.毁荡, ActionType.Gcd, ActionTargetType.Target);
    }
}
