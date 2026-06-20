using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon.Action.OffGcd;

public class 高跳OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentAttackRange = GameData.GetCurrentAttackRange(20);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        // QT控制
        if (!PromeSettings.Instance.GetQt(DRGQt.高跳)) return new CheckResult(false, "已关闭高跳");
        
        if (DRGSkill.高跳.GetActionCooldown() == 0 && Core.Me.DistanceToMe() < CurrentAttackRange)
        {
            return new CheckResult(true, "所有条件满足 && 冷却完毕");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        if (Core.Me.Level >= 74) return new PAction(DRGSkill.高跳, ActionType.OffGcd, ActionTargetType.Target);
        return new PAction(DRGSkill.跳跃, ActionType.OffGcd, ActionTargetType.Target);
    }
}
