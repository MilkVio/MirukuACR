using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;
using MilkVio.DPS.RedMage.RDMData;


namespace MilkVio.DPS.RedMage.Action.OffGcd;

public class 飞刺OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(RDMQt.飞刺反击)) return new CheckResult(false, "未开启飞刺反击");
        
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        // 自身不可以的条件
        if (RDMSkill.飞刺.GetActionCooldown() <= 0.05f) return new CheckResult(true, "打一个");
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(RDMSkill.飞刺, ActionType.OffGcd, ActionTargetType.Target);
    }
}
