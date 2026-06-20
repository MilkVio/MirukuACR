using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.Healer.WHM.WHMData;

namespace MilkVio.Healer.WHM.Action.OGCD;

public class 神速咏唱OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(20);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");

        if (WHMSkill.神速咏唱.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        if (PromeSettings.Instance.GetQt(WHMQt.不打120)) return new CheckResult(false, "未开启QT");
        
        return new CheckResult(true, "打一个");
    }

    public PAction GetAction()
    {
        return new PAction(WHMSkill.神速咏唱, ActionType.OffGcd, ActionTargetType.Self);
    }
}
