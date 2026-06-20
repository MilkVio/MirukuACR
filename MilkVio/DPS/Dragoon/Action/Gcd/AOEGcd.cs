using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon.Action.Gcd;

public class AOEGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = GameData.GetCurrentAttackRange(10);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        
        // Qt控制
        if (!PromeSettings.Instance.GetQt(DRGQt.AOE)) return new CheckResult(false, "未开启AOE");

        if (TargetHelper.EnemyInRange(10) > 3)
        {
            return new CheckResult(true, "敌人数量足够");
        }
        
        return new CheckResult(false, "当前敌人数量不足");
    }

    public PAction GetAction()
    {
        return DragoonHelper.GetNextAOEComboPAction();
    }
}
