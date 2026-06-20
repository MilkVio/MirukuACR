using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;

namespace MilkVio.DPS.Dancer.Action.OffGcd;

public class 探戈OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var 探戈CD = DNCSkill.探戈.GetActionCooldown(); 
        var me = Core.Me;
        if (探戈CD == 0 && (me.HasStatus(DNCBuff.标准舞步) == false && me.HasStatus(DNCBuff.技巧舞步) == false))
        {
            if (DNCSkill.技巧舞步.GetActionCooldown() > 100)
            {
                return new CheckResult(true, "大舞跳完了");
            }
        }

        return new CheckResult(false, "当前不满足任何条件");
    }
    
    public PAction GetAction()
    {
        return new PAction(DNCSkill.探戈, ActionType.OffGcd, ActionTargetType.Self);
    }
}
