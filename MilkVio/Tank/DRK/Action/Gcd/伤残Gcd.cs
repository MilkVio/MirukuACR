using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK.Action.Gcd;

public class 伤残Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() < GameData.GetCurrentMeleeRange()) return new CheckResult(false, "当前目标过近（<3m）");
        
        // QT控制
        if (!PromeSettings.Instance.GetQt(DRKQt.伤残)) return new CheckResult(false, "未开启远离伤残");
        
        if (Core.Me.DistanceToMe() > GameData.GetCurrentMeleeRange() && Core.Me.DistanceToMe() <= 20f)
        {
            return new CheckResult(true, "距离 > 3");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(DRKSkill.伤残, ActionType.Gcd, ActionTargetType.Target);
    }
}
