using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.RedMage.RDMData;

namespace MilkVio.DPS.RedMage.Action.Gcd;

public class 赤核爆神圣Gcd: IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");

        if (JobGaugeHelper.RDM.ManaStacks == 3)
        {
            return new CheckResult(true, "可以打核爆神圣");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if (RedMageHelper.GetLowestManaType() == ManaType.WhiteMana)
        {
            return new PAction(RDMSkill.赤神圣, ActionType.Gcd, ActionTargetType.Target);
        }
        return new PAction(RDMSkill.赤核爆, ActionType.Gcd, ActionTargetType.Target);
    }
}
