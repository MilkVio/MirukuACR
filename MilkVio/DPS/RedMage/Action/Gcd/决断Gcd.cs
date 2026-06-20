using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.RedMage.RDMData;

namespace MilkVio.DPS.RedMage.Action.Gcd;

public class 决断Gcd: IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (me.Level < 90) return new CheckResult(false, "该等级无法发动");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        var lastComboId = ActionHelper.GetLastComboID();
        if (lastComboId == RDMSkill.焦热)
        {
            return new CheckResult(true, "可以打决断");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(RDMSkill.决断, ActionType.Gcd, ActionTargetType.Target);
    }
}
