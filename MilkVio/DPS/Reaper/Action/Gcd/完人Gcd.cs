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

        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");

        if (me.DistanceToMe() > GameData.GetCurrentAttackRange(25)) return new CheckResult(false, "当前目标过远（>25m）");

        if (!ReaperBattleData.Instance.Planner.Current.IsDump && PromeSettings.Instance.GetQt(ReaperQt.远离完人))
        {
            if (ReaperHelper.自身神秘环剩余() > 0) return new CheckResult(true, "神秘环内兑现完人");
            // buff快过期了 不管远离与否都释放
            if (me.GetStatusLeftTime(ReaperBuff.完人预备Buff) < ReaperSettings.Instance.远离完人极限释放阈值)
                return new CheckResult(true, "完人预备即将过期 直接释放");
            if (me.DistanceToMe() > currentMeleeRange)
                return new CheckResult(true, "远离近战距离 释放完人");
            return new CheckResult(false, "近战距离内 留着完人");
        }

        return new CheckResult(true, "有完人预备 直接释放");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.完人, ActionType.Gcd, ActionTargetType.Target);
    }
}
