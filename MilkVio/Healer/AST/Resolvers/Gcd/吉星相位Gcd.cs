using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.Gcd;

public sealed class 吉星相位Gcd : IDecisionResolver
{
    private readonly AstrologianRotationContext _context;

    public 吉星相位Gcd(AstrologianRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (MioAcrSettings.Instance.AstSlideMode != 2) return HealerUtils.Fail("未选择滑步相位吉星");
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        if (!HealerUtils.IsMovingWithoutInstant()) return HealerUtils.Fail("当前不需要滑步相位吉星");

        var me = HealerUtils.Me;
        if (me == null) return HealerUtils.Fail("自身未加载");
        if (me.CurrentMp < 400) return HealerUtils.Fail("魔力不足");
        if (!HealerUtils.TryGetLowestParty(95f, out var target)) return HealerUtils.Fail("没有相位吉星目标");

        _context.SingleHealTarget = target;
        return HealerUtils.Pass("滑步相位吉星");
    }

    public PAction GetAction() => HealerUtils.Gcd(AstAction.吉星相位, _context.SingleHealTarget);
}
