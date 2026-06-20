using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.OffGcd;

public sealed class 能量吸收OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.能量吸收)) return HealerUtils.Fail("能量吸收QT关闭");
        if (!HealerUtils.TrySchAetherflow(out var aetherflow)) return HealerUtils.Fail("Prome SCH以太读取失败");
        var reserve = Math.Clamp(MioAcrSettings.Instance.SchAetherflowReserve, 0, 3);
        if (aetherflow <= reserve) return HealerUtils.Fail($"保留以太：{reserve}，当前以太：{aetherflow}");
        if (!HealerUtils.BattleTimeAtLeast(5)) return HealerUtils.Fail("开场5秒内保留以太");
        var targetCheck = HealerUtils.RequireEnemyTarget(25f);
        if (!targetCheck.Success) return targetCheck;

        return HealerUtils.IsReady(SchAction.能量吸收, 45) ? HealerUtils.Pass("卸以太") : HealerUtils.Fail("能量吸收不可用");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SchAction.能量吸收, ActionTargetType.Target);
}
