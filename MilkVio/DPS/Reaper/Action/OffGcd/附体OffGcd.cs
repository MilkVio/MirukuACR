using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.OffGcd;

public class 附体OffGcd : IDecisionResolver
{
    // 只负责控制夜游魂衣 附体可用层数>=1就打
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ReaperQt.附体)) return new CheckResult(false, "未开启附体");
        if (ReaperHelper.IsIn附体()) return new CheckResult(false, "当前已在附体");
        if (ReaperHelper.IsIn妖异之镰()) return new CheckResult(false, "先消费妖异之镰/处刑");

        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (me.HasStatus(ReaperBuff.完人预备Buff)) return new CheckResult(false, "有完人预备 先打完人");

        if (ReaperSkill.夜游魂衣.GetActionCooldown() != 0) return new CheckResult(false, "夜游魂衣未冷却");
        if (ReaperHelper.可用附体层数() < 1) return new CheckResult(false, "没有可用附体层数");
        if (ReaperHelper.需要续连击(ReaperHelper.单附体占用时间()))
            return new CheckResult(false, "附体前先续连击");

        return new CheckResult(true, "附体层数>=1 直接附体");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.夜游魂衣, ActionType.OffGcd, ActionTargetType.Self);
    }
}
