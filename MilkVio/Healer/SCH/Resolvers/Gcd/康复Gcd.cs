using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SCH.Resolvers.Gcd;

public sealed class 康复Gcd : IDecisionResolver
{
    private readonly ScholarRotationContext _context;

    public 康复Gcd(ScholarRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.康复)) return HealerUtils.Fail("康复QT关闭");
        if (HealerUtils.Level < 10) return HealerUtils.Fail("等级不足");
        if (HealerUtils.IsMovingWithoutInstant()) return HealerUtils.Fail("移动中且无即刻");

        var useH1Priority = MioAcrSettings.Instance.SchIsH1;
        if (!HealerUtils.TryGetScholarCleanseTarget(useH1Priority, out var target))
            return HealerUtils.Fail("没有可康复目标");

        _context.CleanseTarget = target;
        return HealerUtils.Pass(useH1Priority ? "康复：H1优先" : "康复：H2优先");
    }

    public PAction GetAction() => HealerUtils.Gcd(RoleAction.康复, _context.CleanseTarget);
}
