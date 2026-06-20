using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.OffGcd;

public class 星空构想OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var player = Core.Me;
        // 自身一定不能打的条件
        if (!JobGaugeHelper.PCT.LandscapeMotifDrawn) return new CheckResult(false, "未拥有风景画");
        if (PCTSkill.风景构想.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        // if (ActionHelper.GetGcdRemain() > 1.2) return new CheckResult(false, "未在GCD后半段");
        
        // Qt控制
        if (!PromeSettings.Instance.GetQt(PCTQt.星空构想)) return new CheckResult(false, "未开启星空构想");

        return new CheckResult(true, "直接开");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.风景构想, ActionType.OffGcd, ActionTargetType.Self);
    }
}
