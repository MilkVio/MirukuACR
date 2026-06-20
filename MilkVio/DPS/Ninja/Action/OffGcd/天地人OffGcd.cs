using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.OffGcd;

public class 天地人OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (me.HasStatus(NinjaBuff.生杀)) return new CheckResult(false, "身上有生杀");
        
        var cd = NinjaSkill.天地人.GetActionCooldown();
        if (cd == 0)
        {
            if (!PromeSettings.Instance.GetQt(NinjaQt.不打120))
            {
                if (NinjaHelper.Is120() && NinjaHelper.Is60())
                {
                    if (NinjaHelper.GetCurrentNinjaNinjyutsuCharge() > 1.75f)
                    {
                        return new CheckResult(false, "先打一个忍术");
                    }
                    return new CheckResult(true, "cd==0");
                }
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(NinjaSkill.天地人, ActionType.OffGcd, ActionTargetType.Self);
    }
}
