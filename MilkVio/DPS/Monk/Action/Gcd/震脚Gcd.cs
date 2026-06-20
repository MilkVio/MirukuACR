using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;


namespace MilkVio.DPS.Monk.Action.Gcd;

public class 震脚Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = MonkHelper.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > CurrentMeleeRange) return new CheckResult(false, $"当前目标过远（>{CurrentMeleeRange}m）");
        
        if (Core.Me.HasStatus(MNKBuff.震脚))
        {
            return new CheckResult(true, $"距离 <= {CurrentMeleeRange} && 有震脚");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        /*
         大致分为100秒前和100秒后
        100秒前
            有QT优先打QT
            没QT默认按照3阴打
        100秒后
            有QT优先打QT
            没QT默认不溢出阴阳打
        */
        if (EngageManager.GetBattleTime() <= 100)
        {
            if (PromeSettings.Instance.GetQt(MNKQt.震脚打阴)) return MonkHelper.GetBaseActionPerfect(NadiType.阴, false);
            if (PromeSettings.Instance.GetQt(MNKQt.震脚打阳)) return MonkHelper.GetBaseActionPerfect(NadiType.阳, false);
            // 默认三阴起手
            return MonkHelper.GetBaseActionPerfect(NadiType.阴, false);
        }

        if (EngageManager.GetBattleTime() > 100)
        {
            if (PromeSettings.Instance.GetQt(MNKQt.震脚打阴)) return MonkHelper.GetBaseActionPerfect(NadiType.阴, false);
            if (PromeSettings.Instance.GetQt(MNKQt.震脚打阳)) return MonkHelper.GetBaseActionPerfect(NadiType.阳, false);
            // 默认三阴起手
            return MonkHelper.GetBaseActionPerfect(NadiType.无参数, false);
        }
        
        return null;
    }
}
