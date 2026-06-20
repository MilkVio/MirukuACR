using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.WAR.WARData;

namespace MilkVio.Tank.WAR.Action.OffGcd;

public class 动乱OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        // Qt控制
        if (PromeSettings.Instance.GetQt(WARQt.攒资源)) return new CheckResult(false, "已开启不打60");
        
        if (WARSkill.动乱.GetActionCooldown() == 0 && Core.Me.DistanceToMe() <= currentMeleeRange)
        {
            if (PromeSettings.Instance.GetQt(WARQt.优先续红斩) && !Core.Me.HasStatus(WARBuff.战场风暴) && !PromeSettings.Instance.GetQt(WARQt.最终爆发))
            {
                return new CheckResult(false, "续红斩");
            }
            return new CheckResult(true, $"冷却好了");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(WARSkill.动乱, ActionType.OffGcd, ActionTargetType.Target);
    }
}
