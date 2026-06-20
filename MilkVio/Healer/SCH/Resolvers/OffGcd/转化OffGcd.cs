using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.OffGcd;

public sealed class 转化OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.转化)) return HealerUtils.Fail("转化QT关闭");
        var me = HealerUtils.Me;
        if (me == null) return HealerUtils.Fail("自身未加载");
        if (me.HasStatus(SchBuff.转化)) return HealerUtils.Fail("已在转化中");
        if (HealerUtils.SchHasPet() == false) return HealerUtils.Fail("没有仙女");
        if (!HealerUtils.TrySchAetherflow(out var aetherflow)) return HealerUtils.Fail("Prome SCH以太读取失败");
        if (aetherflow > 0) return HealerUtils.Fail($"以太还有层数：{aetherflow}");
        if (MioAcrSettings.Instance.SchResourceOpenerIndex == 1
            && HealerUtils.BattleTimeSeconds < 5
            && HealerUtils.IsReady(SchAction.以太超流, 45))
            return HealerUtils.Fail("以太起手前两GCD保留");
        return HealerUtils.IsReady(SchAction.转化, 60) ? HealerUtils.Pass("转化") : HealerUtils.Fail("转化未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SchAction.转化, ActionTargetType.Self);
}
