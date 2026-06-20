using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon.Action.OffGcd;

public class 龙炎冲OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentAttackRange = GameData.GetCurrentAttackRange(20);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        // QT控制
        if (!PromeSettings.Instance.GetQt(DRGQt.龙炎冲)) return new CheckResult(false, "已关闭龙炎冲");
        
        if (DRGSkill.龙炎冲.GetActionCooldown() == 0 && Core.Me.DistanceToMe() < CurrentAttackRange)
        {
            return new CheckResult(true, "所有条件满足 && 冷却完毕");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(DRGSkill.龙炎冲, ActionType.OffGcd, ActionTargetType.Target);
    }
}
