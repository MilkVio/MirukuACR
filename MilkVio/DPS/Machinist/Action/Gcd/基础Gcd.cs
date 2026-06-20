using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Action.Gcd;

public class 基础Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var me = Core.Me;
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");

        if (me.HasStatus(MCHStatus.过热) || me.HasStatus(MCHStatus.整备))
            return new CheckResult(false, $"当前有过热和整备");
        
        return new CheckResult(true, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return MachinistHelper.GetBaseAction();
    }
}
