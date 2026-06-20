using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;


namespace MilkVio.DPS.Monk.Action.Gcd;

public class 震脚AOEGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = 5;
        // if (Core.Target == null) return new CheckResult(false, "当前无目标");
        // if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        // if (Core.Me.DistanceToMe() > CurrentMeleeRange) return new CheckResult(false, $"当前目标过远（>{CurrentMeleeRange}m）");
        
        // QT控制
        if (!PromeSettings.Instance.GetQt(MNKQt.AOE) && !PromeSettings.Instance.GetQt(MNKQt.无目标搓必杀技)) return new CheckResult(false, "未开启AOE");
        
        if (Core.Me.HasStatus(MNKBuff.震脚) && TargetHelper.EnemyInRange(5) >= 3 && PromeSettings.Instance.GetQt(MNKQt.AOE))
        {
            return new CheckResult(true, $"距离 <= {CurrentMeleeRange} && 有震脚 && 敌人数量够");
        }
        
        if (Core.Me.HasStatus(MNKBuff.震脚) && PromeSettings.Instance.GetQt(MNKQt.无目标搓必杀技))
        {
            if (Core.Target != null && TargetHelper.EnemyInRange(5) == 0) return new CheckResult(false, $"当前有目标");
            return new CheckResult(true, $"当前无目标且周围无敌人");
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
            if (PromeSettings.Instance.GetQt(MNKQt.震脚打阴)) return MonkHelper.GetBaseActionPerfect(NadiType.阴, true);
            if (PromeSettings.Instance.GetQt(MNKQt.震脚打阳)) return MonkHelper.GetBaseActionPerfect(NadiType.阳, true);
            // 默认三阴起手
            return MonkHelper.GetBaseActionPerfect(NadiType.阴, true);
        }

        if (EngageManager.GetBattleTime() > 100)
        {
            if (PromeSettings.Instance.GetQt(MNKQt.震脚打阴)) return MonkHelper.GetBaseActionPerfect(NadiType.阴, true);
            if (PromeSettings.Instance.GetQt(MNKQt.震脚打阳)) return MonkHelper.GetBaseActionPerfect(NadiType.阳, true);
            // 默认三阴起手
            return MonkHelper.GetBaseActionPerfect(NadiType.无参数, true);
        }
        
        return null;
    }
}
