using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon.Action.OffGcd;

public class 武神枪OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentAttackRange = GameData.GetCurrentAttackRange(15);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        // QT控制
        if (PromeSettings.Instance.GetQt(DRGQt.不打60)) return new CheckResult(false, "已开启不打60");
        
        if (DRGSkill.武神枪.GetActionCooldown() == 0 && Core.Me.DistanceToMe() < CurrentAttackRange)
        {
            // 分支
            return new CheckResult(true, "所有条件满足 && 冷却完毕");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(DRGSkill.武神枪, ActionType.OffGcd, ActionTargetType.Target);
    }
}
