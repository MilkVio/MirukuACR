using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon.Action.OffGcd;

public class 战斗连祷OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        // QT控制
        if (PromeSettings.Instance.GetQt(DRGQt.不打120)) return new CheckResult(false, "已开启不打120");
        
        //  这里预计分支控制 已强制对其爆发为分支
        if (DRGSkill.战斗连祷.GetActionCooldown() == 0)
        {
            // 分支
            return new CheckResult(true, "所有条件满足 && 冷却完毕");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(DRGSkill.战斗连祷, ActionType.OffGcd, ActionTargetType.Self);
    }
}
