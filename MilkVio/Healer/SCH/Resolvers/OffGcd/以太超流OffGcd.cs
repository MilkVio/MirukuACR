using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.OffGcd;

public sealed class 以太超流OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.以太超流)) return HealerUtils.Fail("以太超流QT关闭");
        if (!HealerUtils.TrySchAetherflow(out var aetherflow)) return HealerUtils.Fail("Prome SCH以太读取失败");
        if (aetherflow > 0) return HealerUtils.Fail($"以太还有层数：{aetherflow}");
        if (MioAcrSettings.Instance.SchResourceOpenerIndex == 0
            && HealerUtils.BattleTimeSeconds < 5
            && HealerUtils.IsReady(SchAction.转化, 60))
            return HealerUtils.Fail("转化起手前两GCD保留");
        return HealerUtils.IsReady(SchAction.以太超流, 45) ? HealerUtils.Pass("补以太") : HealerUtils.Fail("以太超流未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SchAction.以太超流, ActionTargetType.Self);
}
