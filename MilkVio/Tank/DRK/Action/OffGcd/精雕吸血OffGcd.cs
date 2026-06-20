using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK.Action.OffGcd;

public class 精雕吸血OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() >= GameData.GetCurrentMeleeRange()) return new CheckResult(false, "当前目标过远（>3m）");
        
        // QT控制
        if (PromeSettings.Instance.GetQt(DRKQt.不打60)) return new CheckResult(false, "已开启不打60");
        
        if (Core.Me.DistanceToMe() <= GameData.GetCurrentMeleeRange())
        {
            if (DRKSkill.精雕怒斩.GetActionCooldown() == 0)
            {
                return new CheckResult(true, "距离 < 3");
            }
            
            return new CheckResult(false, "未冷却");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(DRKSkill.精雕怒斩, ActionType.OffGcd, ActionTargetType.Target);
    }
}
