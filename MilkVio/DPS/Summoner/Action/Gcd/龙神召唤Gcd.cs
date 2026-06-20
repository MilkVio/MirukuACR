using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.Gcd;

public class 龙神召唤Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(SMNQt.龙神召唤)) return new CheckResult(false, "未开启QT");
        
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        // 决策不允许的条件
        if (!JobGaugeHelper.SMN.HasPet) return new CheckResult(false, "未召唤宝石兽");
        
        if (JobGaugeHelper.SMN.SummonTimerRemaining != 0) return new CheckResult(false, "当前召唤物未消失");
        
        if (SMNSkill.龙神召唤.GetActionCooldown() < 0.5) return new CheckResult(true, "冷却了直接打");
        
        return new CheckResult(false, "当前为满足任何条件");
    }

    public PAction GetAction()
    {
        
        return new PAction(SMNSkill.龙神召唤, ActionType.Gcd, ActionTargetType.Target);
    }
}
