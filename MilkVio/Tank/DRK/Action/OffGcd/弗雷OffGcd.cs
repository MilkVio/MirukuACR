using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK.Action.OffGcd;

public class 弗雷OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        // QT控制
        if (PromeSettings.Instance.GetQt(DRKQt.不打120)) return new CheckResult(false, "已开启不打120");
        
        if (DRKSkill.掠影示现.GetActionCooldown() == 0)
        {
            if (Core.Me.Level >= 80)
            {
                return new CheckResult(true, "当前有目标 && 冷却==0");
            }
            
            return new CheckResult(false, "未习得该技能");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(DRKSkill.掠影示现, ActionType.OffGcd, ActionTargetType.Self);
    }
}
