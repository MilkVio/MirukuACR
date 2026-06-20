using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.OffGcd;

public class 山崩OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");

        if (SMNSkill.山崩.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        
        if (me.HasStatus(SMNBuff.山崩预备)) return new CheckResult(true, "有Buff打一个");
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SMNSkill.山崩, ActionType.OffGcd, ActionTargetType.Target);
    }
}
