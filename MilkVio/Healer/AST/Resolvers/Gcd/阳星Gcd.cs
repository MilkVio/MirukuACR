using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.Gcd;

public sealed class 阳星Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.治疗)) return HealerUtils.Fail("治疗QT关闭");
        var me = HealerUtils.Me;
        if (me == null) return HealerUtils.Fail("自身未加载");
        if (me.CurrentMp < 900) return HealerUtils.Fail("魔力不足");
        if (!HealerUtils.CanHardCast()) return HealerUtils.Fail("移动中");
        return HealerUtils.LowPartyCount(58f) >= 3 ? HealerUtils.Pass("阳星群抬") : HealerUtils.Fail("群体血量安全");
    }

    public PAction GetAction() => HealerUtils.Gcd(AstAction.阳星, ActionTargetType.Self);
}
