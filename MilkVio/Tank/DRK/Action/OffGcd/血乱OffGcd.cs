using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK.Action.OffGcd;

public class 血乱OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        // QT控制
        if (PromeSettings.Instance.GetQt(DRKQt.不打60)) return new CheckResult(false, "已开启不打60");
        
        if (DRKSkill.血乱.GetActionCooldown() == 0)
        {
            // 溢出控制
            if (JobGaugeHelper.DRK.Blood > 70) return new CheckResult(false, "暗血溢出");
            
            return new CheckResult(true, "当前有目标 && 冷却==0");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(DRKSkill.血乱, ActionType.OffGcd, ActionTargetType.Self);
    }
}
