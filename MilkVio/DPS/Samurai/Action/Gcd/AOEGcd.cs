using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Action.Gcd;

public class AOEGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var enemyCount = TargetHelper.EnemyIn5m();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (!PromeSettings.Instance.GetQt(SAMQt.AOE)) return new CheckResult(false, "未开启AOEQT");
        
        if (enemyCount >= 3)
        {
            return new CheckResult(true, $">3");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var lastActionId = ActionHelper.GetLastComboID();
        var has花 = JobGaugeHelper.SAM.HasHana;
        var has月 = JobGaugeHelper.SAM.HasMoon;
        
        if (lastActionId == SamuraiHelper.Get1ComboActionId(true))
        {
            if (!has月)
            {
                return new PAction(SAMSkill.满月, ActionType.Gcd, ActionTargetType.Self);
            }
            if (!has花)
            {
                return new PAction(SAMSkill.樱花, ActionType.Gcd, ActionTargetType.Self);
            }
        }

        if (SamuraiHelper.Get1ComboActionId(true) == SAMSkill.风雅)
        {
            return new PAction(SamuraiHelper.Get1ComboActionId(true), ActionType.Gcd, ActionTargetType.Target);
        }
        
        return new PAction(SamuraiHelper.Get1ComboActionId(true), ActionType.Gcd, ActionTargetType.Self);
    }
}
