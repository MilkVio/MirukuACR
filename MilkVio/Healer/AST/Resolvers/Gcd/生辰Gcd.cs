using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.Gcd;

public sealed class 生辰Gcd : IDecisionResolver
{
    private readonly AstrologianRotationContext _context;

    public 生辰Gcd(AstrologianRotationContext context)
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
        if (me.CurrentMp < 2400) return HealerUtils.Fail("魔力不足");
        if (!me.HasStatus(RoleBuff.即刻咏唱) && !me.HasStatus(AstBuff.光速) && !HealerUtils.CanHardCast()) return HealerUtils.Fail("移动中且无即刻/光速");
        return HealerUtils.Level >= 12 ? HealerUtils.Pass("生辰") : HealerUtils.Fail("等级不足");
    }

    public PAction GetAction() => HealerUtils.Gcd(AstAction.生辰, _context.RaiseTarget);
}
