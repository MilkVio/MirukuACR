using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.Gcd;

public sealed class 毁坏Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.毁2)) return HealerUtils.Fail("毁2 QT关闭");
        if (!PromeRotation.Managers.MoveManager.IsLocalPlayerMoving) return HealerUtils.Fail("未移动");
        var targetCheck = HealerUtils.RequireEnemyTarget(25f, 400);
        if (!targetCheck.Success) return targetCheck;
        return HealerUtils.Level >= 38 ? HealerUtils.Pass("移动毁坏") : HealerUtils.Fail("等级不足");
    }

    public PAction GetAction() => HealerUtils.Gcd(SchAction.毁坏, ActionTargetType.Target);
}
