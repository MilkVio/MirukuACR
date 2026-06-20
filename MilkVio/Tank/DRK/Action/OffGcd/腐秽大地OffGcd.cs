using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK.Action.OffGcd;

public class 腐秽大地OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > 5f) return new CheckResult(false, "当前目标过远（>5m）");
        
        // QT判断
        if (PromeSettings.Instance.GetQt(DRKQt.马桶对齐120))
        {
            if (DRKSkill.腐秽大地.GetActionCooldown() == 0 && JobGaugeHelper.DRK.ShadowTimeRemaining == 0)
            {
                return new CheckResult(false, "已开启马桶对齐120 目前没有进入120");
            }
        }
        
        if (Core.Me.DistanceToMe() <= 5f)
        {
            if (DRKSkill.腐秽大地.GetActionCooldown() == 0)
            {
                return new CheckResult(true, "距离 < 5");
            }
            
            return new CheckResult(false, "未冷却");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var target = Core.Target;
        return new PAction(DRKSkill.腐秽大地, ActionType.OffGcd, ActionTargetType.Self);
    }
}
