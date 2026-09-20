using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;
using MilkVio.DPS.Samurai.Level100;

namespace MilkVio.DPS.Samurai.Action.OffGcd;

public class 照破OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (!PromeSettings.Instance.GetQt(SAMQt.照破)) return new CheckResult(false, "未开启照破QT");
        if (Samurai100Helper.Enabled && Core.Me.DistanceToMe() > GameData.GetCurrentAttackRange(10))
            return new CheckResult(false, "照破距离不足");
        var isCanUse = SAMSkill.照破.GetActionCooldown() == 0 && JobGaugeHelper.SAM.剑压 == 3;
        
        if (isCanUse)
        {
            if (Samurai100Helper.Enabled && Samurai100Planning.TryOff(SAMSkill.照破, out var use, out var reason))
                return new CheckResult(use, reason);
            return new CheckResult(true, $"好了就用");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SAMSkill.照破, ActionType.OffGcd, ActionTargetType.Target);
    }
}
