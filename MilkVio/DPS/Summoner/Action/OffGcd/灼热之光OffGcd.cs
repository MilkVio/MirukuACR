using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.OffGcd;

public class 灼热之光OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt(SMNQt.不打120)) return new CheckResult(false, "未开启QT");
        
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");

        if (!JobGaugeHelper.SMN.HasPet) return new CheckResult(false, "未召唤宝石兽");
        if (SMNSkill.灼热之光.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        
        return new CheckResult(true, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SMNSkill.灼热之光, ActionType.OffGcd, ActionTargetType.Target);
    }
}
