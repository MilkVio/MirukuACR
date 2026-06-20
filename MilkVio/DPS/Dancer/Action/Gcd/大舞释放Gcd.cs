using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;
namespace MilkVio.DPS.Dancer.Action.Gcd;

public class 大舞释放Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var me = Core.Me;
        var 大舞CD = DNCSkill.技巧舞步.GetActionCooldown();
        if (me == null) return new CheckResult(false, "我不存在");
        var gcd和目标距离 = GameData.GetCurrentAttackRange(25f);
        
        //强制大舞
        if (PromeSettings.Instance.GetQt(DNCQt.强制大舞) && 大舞CD <= 1 )
        {
            return new CheckResult(true, "强制大舞");
        }

        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (PromeSettings.Instance.GetQt(DNCQt.不打120)) return new CheckResult(false, "不打120");
        

        if (大舞CD <= 1 && me.HasStatus(DNCBuff.标准舞步结束))
        {
            if (Core.Me.DistanceToMe() <= gcd和目标距离)
            {
                return new CheckResult(true, "距离 <= 15");
            }
            return new CheckResult(false, "大舞CD/标准舞步结束buff 没合格");
        }
        if (大舞CD <= 1 && PromeSettings.Instance.GetQt(DNCQt.先打大舞) == true)
        {
            if (Core.Me.DistanceToMe() <= gcd和目标距离)
            {
                return new CheckResult(true, "距离 <= 15");
            }
            return new CheckResult(false, "大舞CD/先打大舞QT 没合格");
        }
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(DNCSkill.技巧舞步, ActionType.Gcd, ActionTargetType.Self);
    } 
}
