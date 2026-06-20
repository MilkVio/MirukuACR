using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.Gcd;

public sealed class 注药Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (HealerUtils.SgeEukrasia()) return HealerUtils.Fail("已有均衡，等待均衡技能");
        var targetCheck = HealerUtils.RequireEnemyTarget(25f, 400, false);
        if (!targetCheck.Success) return targetCheck;
        return HealerUtils.Pass("基础填充");
    }

    public PAction GetAction() => HealerUtils.Gcd(SgeAction.注药, ActionTargetType.Target);
}
