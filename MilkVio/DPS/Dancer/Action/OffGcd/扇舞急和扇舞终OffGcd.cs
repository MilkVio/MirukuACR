using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;

namespace MilkVio.DPS.Dancer.Action.OffGcd;

public class 扇舞急和扇舞终OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        
        var gcd和目标距离 = GameData.GetCurrentAttackRange(25f);
        var me = Core.Me;
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        if (DNCSkill.技巧舞步.GetActionCooldown() > 100 && DNCSkill.探戈.GetActionCooldown() == 0)
            return new CheckResult(false, "探戈没打出来");
        
        if (Core.Me.DistanceToMe() <= gcd和目标距离 && me.HasStatus(DNCBuff.标准舞步) == false && me.HasStatus(DNCBuff.技巧舞步) == false)
        {
            

            if (me.HasStatus(DNCBuff.扇舞急预备)||me.HasStatus(DNCBuff.扇舞终预备)==true)
            {
                return new CheckResult(true, "距离 <= 25");
            }

            return new CheckResult(false, "距离不够");
        }

        return new CheckResult(false, "当前不满足任何条件");
    }
    
    public PAction GetAction()
    {
        var sw = Core.Me;
        if (sw.HasStatus(DNCBuff.扇舞急预备))
        {
            return new PAction(DNCSkill.扇舞急, ActionType.OffGcd, ActionTargetType.Target);
        }
        return new PAction(DNCSkill.扇舞终, ActionType.OffGcd, ActionTargetType.Target);
    }
}


