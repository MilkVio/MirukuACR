using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Summoner.Action.OffGcd;

public class 醒梦OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var player = Core.Me;
        if (player == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == player.EntityId) return new CheckResult(false, "当前目标为自己");
        
        var canUse = PCTSkill.醒梦.GetActionCooldown() == 0 && player.CurrentMp < 7000;
        
        if (canUse)
        {
            return new CheckResult(true, "魔力不足");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.醒梦, ActionType.OffGcd, ActionTargetType.Self);
    }
}
