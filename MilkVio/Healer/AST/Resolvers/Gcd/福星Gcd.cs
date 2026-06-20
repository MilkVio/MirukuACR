using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.Gcd;

public sealed class 福星Gcd : IDecisionResolver
{
    private readonly AstrologianRotationContext _context;

    public 福星Gcd(AstrologianRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        var me = HealerUtils.Me;
        if (me == null) return HealerUtils.Fail("自身未加载");
        if (me.CurrentMp < 700) return HealerUtils.Fail("魔力不足");
        if (!HealerUtils.CanHardCast()) return HealerUtils.Fail("移动中");
        if (!HealerUtils.TryGetLowestParty(35f, out var target)) return HealerUtils.Fail("没有单抬目标");
        _context.SingleHealTarget = target;
        return HealerUtils.Pass("福星单抬");
    }

    public PAction GetAction() => HealerUtils.Gcd(AstAction.福星, _context.SingleHealTarget);
}
