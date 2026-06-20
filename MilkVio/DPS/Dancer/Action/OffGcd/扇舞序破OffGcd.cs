
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;


namespace MilkVio.DPS.Dancer.Action.OffGcd;


public class 扇舞序破OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var 扇子 = JobGaugeHelper.DNC.Feathers;
        var gcd和目标距离 = GameData.GetCurrentAttackRange(25f);
        var 有扇子 = 扇子 >= 1;
        var me = Core.Me;
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        if (DNCSkill.技巧舞步.GetActionCooldown() > 100 && DNCSkill.探戈.GetActionCooldown() == 0)
            return new CheckResult(false, "探戈没打出来");
        
        if (Core.Me.DistanceToMe() <= gcd和目标距离 && (me.HasStatus(DNCBuff.标准舞步) == false && me.HasStatus(DNCBuff.技巧舞步) == false))
        {
            if ((PromeSettings.Instance.GetQt(DNCQt.不打扇子)))
            {
                return new CheckResult(false, "距离 <= 25 但是不打扇子");
            }
        
            if ((PromeSettings.Instance.GetQt(DNCQt.倾泻资源) ||me.HasStatus(DNCBuff.技巧舞步结束) ))
            {
                if(有扇子)return new CheckResult(true, "距离 <= 25 有扇子 大舞/倾泻资源");
            }
            
            if (扇子 > 3)
            {
                return new CheckResult(true, "距离 <= 25 扇子大于3");
            }
            return new CheckResult(false, "扇子不够大于3");
        }
        return new CheckResult(false, "当前不满足任何条件");
    }
       


    

    public PAction GetAction()
    {
        if (TargetHelper.EnemyIn5m() >= 2 && PromeSettings.Instance.GetQt(DNCQt.启用多目标))
        {
                return new PAction(DNCSkill.扇舞破, ActionType.OffGcd, ActionTargetType.Target);
        }
        
        return new PAction(DNCSkill.扇舞序, ActionType.OffGcd, ActionTargetType.Target);
    }
}
        
        
        
        
        


