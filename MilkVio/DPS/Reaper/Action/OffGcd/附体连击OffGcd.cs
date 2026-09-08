using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.OffGcd;

public class 附体连击OffGcd : IDecisionResolver
{
    // 专门处理附体中OffGcd的控制 每次附体尽早打一发夜游魂切割
    public CheckResult Check()
    {
        if (JobGaugeHelper.RPR.虚无魂 < 2) return new CheckResult(false, "虚无魂不足2层");
        if (ReaperSkill.隐匿挥割.GetAdjustedActionId() != ReaperSkill.夜游魂切割) return new CheckResult(false, "隐匿挥割未变为夜游魂切割");

        return new CheckResult(true, "附体中 打夜游魂切割");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.夜游魂切割, ActionType.OffGcd, ActionTargetType.Target);
    }
}
