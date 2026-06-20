using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 即刻复活OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.复活)) return HealerUtils.Fail("复活QT关闭");
        if (!HealerUtils.TryGetDeadParty(out _)) return HealerUtils.Fail("没有死亡队友");
        return HealerUtils.IsReady(RoleAction.即刻咏唱, 18) && HealerUtils.Me is { } me && !me.HasStatus(RoleBuff.即刻咏唱)
            ? HealerUtils.Pass("准备即刻复活")
            : HealerUtils.Fail("即刻不可用");
    }

    public PAction GetAction() => HealerUtils.OffGcd(RoleAction.即刻咏唱, ActionTargetType.Self);
}
