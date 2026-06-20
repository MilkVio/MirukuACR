using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;


namespace MilkVio.DPS.Dragoon.Action.Gcd;

public class 连击2Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = GameData.GetCurrentMeleeRange();
        
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > CurrentMeleeRange) return new CheckResult(false, $"当前目标过远（>{CurrentMeleeRange}m）");
        
        // 自身不可以的条件
        
        
        if (Core.Me.DistanceToMe() <= CurrentMeleeRange)
        {
            if (ActionHelper.GetLastComboID() == DRGSkill.精准刺 || ActionHelper.GetLastComboID() == DRGSkill.龙眼雷电)
            {
                return new CheckResult(true, "准备求解2连");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var notHasDragonGun = !Core.Me.HasStatus(DRGBuff.龙枪) || StatusHelper.GetStatusLeftTime(Core.Me, DRGBuff.龙枪) < 10;
        if (PromeSettings.Instance.GetQt(DRGQt.只打樱花连)) return DragoonHelper.GetCurrentSakuraComboPAction(2);
        if (PromeSettings.Instance.GetQt(DRGQt.只打直刺连)) return DragoonHelper.GetCurrentStraightComboPAction(2);
        if(notHasDragonGun) return DragoonHelper.GetCurrentSakuraComboPAction(2);
        return DragoonHelper.GetCurrentStraightComboPAction(2);
    }
}
