using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.Gcd;

public sealed class 箭毒Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.箭毒)) return HealerUtils.Fail("箭毒QT关闭");
        if (HealerUtils.SgeAddersting() == 0) return HealerUtils.Fail("没有蛇刺");
        if (!PromeRotation.Managers.MoveManager.IsLocalPlayerMoving) return HealerUtils.Fail("未移动，保留箭毒");
        var targetCheck = HealerUtils.RequireEnemyTarget(25f);
        if (!targetCheck.Success) return targetCheck;
        return HealerUtils.Level >= 66 ? HealerUtils.Pass("箭毒") : HealerUtils.Fail("等级不足");
    }

    public PAction GetAction() => HealerUtils.Gcd(SgeAction.箭毒, ActionTargetType.Target);
}
