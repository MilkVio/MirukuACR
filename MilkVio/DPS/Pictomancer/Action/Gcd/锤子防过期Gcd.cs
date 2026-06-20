using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.Gcd;

public class 锤子防过期Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        var player = Core.Me;
        
        if (Core.Me.DistanceToMe() <= currentAttackRange && player.HasStatus(PCTBuff.重锤连击))
        {
            var stackTime = 2.5f;
            var stack = StatusHelper.GetStatusStack(player, PCTBuff.重锤连击);
            if (StatusHelper.GetStatusLeftTime(player, PCTBuff.重锤连击) < stackTime * stack)
            {
                return new CheckResult(true, "即将溢出");
            }
            return new CheckResult(false, "没有溢出");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return PctHelper.GetHammerGcd();
    }
}
