using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.OffGcd;

public sealed class 连环计OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.连环计)) return HealerUtils.Fail("连环计QT关闭");
        var targetCheck = HealerUtils.RequireEnemyTarget(25f);
        if (!targetCheck.Success) return targetCheck;
        if (!HealerUtils.IsReady(SchAction.连环计, 66)) return HealerUtils.Fail("连环计未冷却");

        var battleTime = HealerUtils.BattleTimeSeconds;
        if (battleTime < 10)
        {
            var minTime = MioAcrSettings.Instance.SchSwiftOpenerIndex == 0 ? 5.2 : 5.0;
            return battleTime >= minTime
                ? HealerUtils.Pass("起手连环计")
                : HealerUtils.Fail("起手连环计窗口未到");
        }

        return HealerUtils.Pass("连环计");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SchAction.连环计, ActionTargetType.Target);
}
