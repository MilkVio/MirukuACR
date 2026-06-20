using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.Tank.WAR.WARData;

namespace MilkVio.Tank.WAR.Action.Gcd;

public class 飞斧Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        // Qt
        if (!PromeSettings.Instance.GetQt(WARQt.飞斧)) return new CheckResult(false, "未开启飞斧");
        
        if (Core.Me.DistanceToMe() > currentMeleeRange)
        {
            return new CheckResult(true, $"距离 > {currentMeleeRange}");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(WARSkill.飞斧, ActionType.Gcd, ActionTargetType.Target);
    }
}
