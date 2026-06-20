using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.Tank.WAR.WARData;

namespace MilkVio.Tank.WAR.Action.Gcd;

public class 蛮荒崩裂Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(20);
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        // Qt
        if (!PromeSettings.Instance.GetQt(WARQt.蛮荒崩裂)) return new CheckResult(false, "未开启蛮荒崩裂");
        
        if (Core.Me.DistanceToMe() <= currentAttackRange)
        {
            if (Core.Me.HasStatus(WARBuff.蛮荒崩裂预备))
            {
                return new CheckResult(true, $"距离 < {currentAttackRange}");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(WARSkill.蛮荒崩裂, ActionType.Gcd, ActionTargetType.Target);
    }
}
