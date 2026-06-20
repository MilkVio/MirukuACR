using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;
using MilkVio.DPS.RedMage.RDMData;


namespace MilkVio.DPS.RedMage.Action.Gcd;

public class 魔连攻Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // QT
        if (PromeSettings.Instance.GetQt(RDMQt.不打魔连击)) return new CheckResult(false, "未开启QT");
        
        var currentAttackRange = GameData.GetCurrentAttackRange(3);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.HasStatus(RDMBuff.魔元化Buff)) currentAttackRange = GameData.GetCurrentAttackRange(25);
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");

        if (RedMageHelper.GetNextManaActionPhase(false) == 3) return new CheckResult(true, "打3");
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(RDMSkill.魔连攻3, ActionType.Gcd, ActionTargetType.Target);
    }
}
