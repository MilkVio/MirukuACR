using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.DPS.Reaper.Action.Gcd;

public class 连击保护Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!ReaperHelper.有近战目标()) return new CheckResult(false, "没有近战目标");
        if (ReaperHelper.IsIn附体() || ReaperHelper.IsIn妖异之镰())
            return new CheckResult(false, "先完成附体/处刑");
        if (ReaperHelper.下一段连击() == 0) return new CheckResult(false, "没有可续的连击");
        return ReaperHelper.需要提前续连击()
            ? new CheckResult(true, "连击将超时 先续连击")
            : new CheckResult(false, "连击时间充足");
    }

    public PAction GetAction() => new(ReaperHelper.下一段连击(), ActionType.Gcd, ActionTargetType.Target);
}
