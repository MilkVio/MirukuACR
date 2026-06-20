using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;
namespace MilkVio.DPS.Dancer.Action.Gcd;

public class 拂晓舞Gcd : IDecisionResolver
{
    // 检查什么时候可以用这个技能
    // 如果检查通过 就执行下面的getaction
    public CheckResult Check()
    {
        var gcd和目标距离 = GameData.GetCurrentAttackRange(25f);
        var me = Core.Me;
        var 伶俐 = JobGaugeHelper.DNC.Esprit;
        var 小舞CD = DNCSkill.标准舞步.GetActionCooldown();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        if (Core.Me.DistanceToMe() <= gcd和目标距离)
        {
            
            if (me.HasStatus(DNCBuff.拂晓舞预备) )
            {
                if (伶俐 >= 50 )
                {
                    return new CheckResult(true, "拂晓舞");
                }
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        
        return new PAction(DNCSkill.拂晓舞, ActionType.Gcd, ActionTargetType.Target);
    }



}
