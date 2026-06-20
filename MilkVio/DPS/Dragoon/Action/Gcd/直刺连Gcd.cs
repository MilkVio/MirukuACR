using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;


namespace MilkVio.DPS.Dragoon.Action.Gcd;

public class 直刺连Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = GameData.GetCurrentMeleeRange();
        var HasDragonGun = StatusHelper.GetStatusLeftTime(Core.Me, DRGBuff.龙枪) > 10;
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > CurrentMeleeRange) return new CheckResult(false, $"当前目标过远（>{CurrentMeleeRange}m）");
        
        // 自身不可以的条件
        if (ActionHelper.GetLastComboID() == DRGSkill.精准刺 || ActionHelper.GetLastComboID() == DRGSkill.龙眼雷电)
            return new CheckResult(false, "当前正在求解");
        
        if (Core.Me.DistanceToMe() <= CurrentMeleeRange)
        {
            return new CheckResult(true, $"距离 <= {CurrentMeleeRange} && 不在求解");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return DragoonHelper.GetNextComboPAction();
    }
}
