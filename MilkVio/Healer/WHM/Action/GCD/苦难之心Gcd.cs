using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.Healer.WHM.WHMData;

namespace MilkVio.Healer.WHM.Action.GCD;

public class 苦难之心Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");

        if (!PromeSettings.Instance.GetQt(WHMQt.红花)) return new CheckResult(false, "当前未开启红花QT");

        if (JobGaugeHelper.WHM.BloodLily == 3) return new CheckResult(true, "打一个");
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(WHMSkill.苦难之心, ActionType.Gcd, ActionTargetType.Target);
    }
}
