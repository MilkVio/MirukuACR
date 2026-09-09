using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.Gcd;

public class 完人Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (!me.HasStatus(ReaperBuff.完人预备Buff)) return new CheckResult(false, "没有完人预备");
        if (ReaperHelper.IsIn附体() || ReaperHelper.IsIn妖异之镰()) return new CheckResult(false, "当前在附体/妖异之镰");

        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");

        if (me.DistanceToMe() > GameData.GetCurrentAttackRange(25)) return new CheckResult(false, "当前目标过远（>25m）");

        return ReaperResources.AllowsPerfectio(ReaperRotation.ReadState())
            ? new CheckResult(true, "当前允许兑现完人")
            : new CheckResult(false, "保留完人或先保护连击");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.完人, ActionType.Gcd, ActionTargetType.Target);
    }
}
