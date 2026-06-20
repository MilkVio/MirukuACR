using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.OffGcd;

public sealed class 心关OffGcd : IDecisionResolver
{
    private readonly SageRotationContext _context;

    public 心关OffGcd(SageRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.心关)) return HealerUtils.Fail("心关QT关闭");
        var me = HealerUtils.Me;
        if (me == null) return HealerUtils.Fail("自身未加载");
        if (me.HasStatus(SgeBuff.心关) || me.HasStatus(SgeBuff.心关新版)) return HealerUtils.Fail("已有心关");
        if (!HealerUtils.IsReady(SgeAction.心关, 4)) return HealerUtils.Fail("心关不可用");

        _context.KardiaTarget = HealerUtils.TryGetLowestTank(100f, out var tank) ? tank : ActionTargetType.Self;
        return HealerUtils.Pass("补心关");
    }

    public PAction GetAction() => HealerUtils.OffGcd(SgeAction.心关, _context.KardiaTarget);
}
