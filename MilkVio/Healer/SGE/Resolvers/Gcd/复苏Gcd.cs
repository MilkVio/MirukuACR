using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.Gcd;

public sealed class 复苏Gcd : IDecisionResolver
{
    private readonly SageRotationContext _context;

    public 复苏Gcd(SageRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.复活)) return HealerUtils.Fail("复活QT关闭");
        if (!HealerUtils.TryGetDeadParty(out var target)) return HealerUtils.Fail("没有死亡队友");
        _context.RaiseTarget = target;

        var me = HealerUtils.Me;
        if (me == null) return HealerUtils.Fail("自身未加载");
        if (HealerUtils.Level < 12) return HealerUtils.Fail("等级不足");
        if (me.CurrentMp < 2400) return HealerUtils.Fail("魔力不足");
        if (!me.HasStatus(RoleBuff.即刻咏唱) && !HealerUtils.CanHardCast()) return HealerUtils.Fail("移动中且无即刻");
        return HealerUtils.Pass("复苏");
    }

    public PAction GetAction() => HealerUtils.Gcd(SgeAction.复苏, _context.RaiseTarget);
}
