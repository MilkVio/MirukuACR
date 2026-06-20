using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Action.OffGcd;

public class 枪管加热OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var cd = MCHSkill.枪管加热.GetActionCooldown();
        if(cd != 0) return new CheckResult(false, "未冷却");

        if (!PromeSettings.Instance.GetQt(MCHQt.枪管加热)) return new CheckResult(false, "未开启QT");

        return new CheckResult(true, "好了");
    }

    public PAction GetAction()
    {
        return new PAction(MCHSkill.枪管加热, ActionType.OffGcd, ActionTargetType.Self);
    }
}
