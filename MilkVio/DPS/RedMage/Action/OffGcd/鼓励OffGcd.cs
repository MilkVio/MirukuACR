using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;
using MilkVio.DPS.RedMage.RDMData;


namespace MilkVio.DPS.RedMage.Action.OffGcd;

public class 鼓励OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt(RDMQt.不打120)) return new CheckResult(false, "未开启QT");
        
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        if (RDMSkill.鼓励.GetActionCooldown() == 0) return new CheckResult(true, "冷却了打掉");
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(RDMSkill.鼓励, ActionType.OffGcd, ActionTargetType.Target);
    }
}
