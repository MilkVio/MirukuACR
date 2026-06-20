using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;
namespace MilkVio.DPS.Dancer.Action.OffGcd;

public class 百花OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var 百花CD = DNCSkill.百花.GetActionCooldown();
        var me = Core.Me;

        if (PromeSettings.Instance.GetQt(DNCQt.不打百花))
        {
            return new CheckResult(false, "不打百花或不打120");
        }

        if (me.Level < 72)
        {
            return new CheckResult(false, "等级不够");
        }
        
        if (DNCSkill.技巧舞步.GetActionCooldown() > 100 && DNCSkill.探戈.GetActionCooldown() == 0)
            return new CheckResult(false, "探戈没打出来");
        
        if (百花CD == 0)
        {
            if ((PromeSettings.Instance.GetQt(DNCQt.不打百花) == false || PromeSettings.Instance.GetQt(DNCQt.不打120) == false))
            {
                if (me.HasStatus(DNCBuff.标准舞步) == false && me.HasStatus(DNCBuff.技巧舞步) == false)
                {
                        return new CheckResult(true, "百花");
                }
            }
        }
        return new CheckResult(false, "当前不满足任何条件");
    }
    
    public PAction GetAction()
    {
        return new PAction(DNCSkill.百花, ActionType.OffGcd, ActionTargetType.Target);
    }
}

