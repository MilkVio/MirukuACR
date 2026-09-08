using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.OffGcd;

public class 神秘环OffGcd : IDecisionResolver
{
    // 团辅技能 自身释放 不受距离控制
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ReaperQt.神秘环)) return new CheckResult(false, "未开启120");
        if (ReaperSkill.神秘环.GetActionCooldown() > 0.2f) return new CheckResult(false, "神秘环未冷却");

        return new CheckResult(true, "团辅好了就打");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.神秘环, ActionType.OffGcd, ActionTargetType.Self);
    }
}
