using System.Collections.Generic;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.OffGcd;

public class 即刻风景画OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (JobGaugeHelper.PCT.LandscapeMotifDrawn) return new CheckResult(false, "当前拥有风景画");
        if (!PromeSettings.Instance.GetQt(PCTQt.即刻风景画)) return new CheckResult(false, "未开启即刻风景画QT");
        
        var canUse = PCTSkill.即刻咏唱.GetActionCooldown() == 0 && !Core.Me.HasStatus(PCTBuff.即刻咏唱) && !Core.Me.HasStatus(PCTBuff.星空构想);
        
        if (canUse)
        {
            return new CheckResult(true, "即刻风景画");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        List<PAction> 即刻风景画 = new List<PAction>
        {
            new(PCTSkill.即刻咏唱, ActionType.OffGcd, ActionTargetType.Self),
            new(PCTSkill.风景彩绘, ActionType.Gcd, ActionTargetType.Self),
        };
        ActionQueueManager.EnqueueOffGcdList(即刻风景画);
        // 不返回任何技能，因为 Group 会自动开始执行
        return null;
    }
}
