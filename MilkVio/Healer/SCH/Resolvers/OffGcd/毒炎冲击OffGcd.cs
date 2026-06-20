using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.OffGcd;

public sealed class 毒炎冲击OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var targetCheck = HealerUtils.RequireEnemyTarget(25f);
        if (!targetCheck.Success) return targetCheck;
        if (HealerUtils.Me is not { } me || !me.HasStatus(SchBuff.毒炎冲击预备)) return HealerUtils.Fail("没有毒炎冲击预备");
        return HealerUtils.IsReady(SchAction.毒炎冲击, 92) ? HealerUtils.Pass("毒炎冲击") : HealerUtils.Fail("毒炎冲击不可用");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SchAction.毒炎冲击, ActionTargetType.Target);
}
