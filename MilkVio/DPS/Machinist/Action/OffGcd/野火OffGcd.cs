using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Action.OffGcd;

public class 野火OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var cd = MCHSkill.野火.GetActionCooldown();
        if(cd != 0) return new CheckResult(false, "未冷却");
        
        if (!PromeSettings.Instance.GetQt(MCHQt.野火)) return new CheckResult(false, "未开启QT");
        var me = Core.Me;
        var isCanUse = cd == 0;

        if (isCanUse)
        {
            var 空气锚cd = MCHSkill.空气锚.GetActionCooldown();
            var 钻头cd = MCHSkill.钻头.GetActionCooldown();
            var 回转飞锯cd = MCHSkill.回转飞锯.GetActionCooldown();
            if (空气锚cd == 0 || 钻头cd == 0 || (me.Level >= 90 && 回转飞锯cd == 0))
            {
                return new CheckResult(false, "有大技能CD");
            }

            if (MachinistHelper.Get超荷Count() >= 1)
            {
                return new CheckResult(true, "好了");
            }
        }
        
        return new CheckResult(false, "no");
    }

    public PAction GetAction()
    {
        return new PAction(MCHSkill.野火, ActionType.OffGcd, ActionTargetType.Target);
    }
}
