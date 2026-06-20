using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;

namespace MilkVio.DPS.Monk.Action.OffGcd;

public class 阴阳斗气斩OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = MonkHelper.GetCurrentMeleeRange();
        var CanUseChakra = MonkHelper.CanUseChakra();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > CurrentMeleeRange) return new CheckResult(false, $"当前目标过远（>{CurrentMeleeRange}m）");

        // QT控制
        // if (PromeSettings.Instance.GetQt(DRKQt.不打120)) return new CheckResult(false, "已开启不打120");
        
        if (CanUseChakra)
        {
            return new CheckResult(true, "当前有目标 && 豆子够");
        }
        
        return new CheckResult(false, "豆子不够");
    }

    public PAction GetAction()
    {
        return new PAction(MNKSkill.阴阳斗气斩, ActionType.OffGcd, ActionTargetType.Target);
    }
}
