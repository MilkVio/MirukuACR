using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Action.Gcd;

public class AOEGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (!PromeSettings.Instance.GetQt(PLDQt.AOE)) return new CheckResult(false, "未开启AOE");
        
        if (TargetHelper.EnemyInRange(5) > 2)
        {
            return new CheckResult(true, "5M内有3个敌人");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var lastActionId = ActionHelper.GetLastComboID();
        if (lastActionId == PLDSkill.全蚀斩)
        {
            return new PAction(PLDSkill.日珥斩, ActionType.Gcd, ActionTargetType.Self);
        }
        return new PAction(PLDSkill.全蚀斩, ActionType.Gcd, ActionTargetType.Self);
    }
}
