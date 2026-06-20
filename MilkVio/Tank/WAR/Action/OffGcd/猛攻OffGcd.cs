using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.Tank.WAR.WARData;

namespace MilkVio.Tank.WAR.Action.OffGcd;

public class 猛攻OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        // Qt
        if (!PromeSettings.Instance.GetQt(WARQt.猛攻)) return new CheckResult(false, "未开启猛攻");
        
        if (Core.Me.DistanceToMe() <= 3)
        {
            if (PromeSettings.Instance.GetQt(WARQt.优先续红斩) && !Core.Me.HasStatus(WARBuff.战场风暴) && !PromeSettings.Instance.GetQt(WARQt.最终爆发))
            {
                return new CheckResult(false, "续红斩");
            }

            if (Core.Me.HasStatus(WARBuff.原初的觉悟) && WARSkill.猛攻.GetActionCharges() >= 1)
            {
                return new CheckResult(true, $"距离 < {5f}");
            }
            
            // 88级之后三层
            if (Core.Me.Level >= 88)
            {
                if (WARSkill.猛攻.GetActionCharges() >= 2)
                {
                    return new CheckResult(true, $"距离 < {3f}");
                }
            }

            if (Core.Me.Level < 88)
            {
                if (WARSkill.猛攻.GetActionCharges() >= 1)
                {
                    return new CheckResult(true, $"距离 < {3f}");
                }
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(WARSkill.猛攻, ActionType.OffGcd, ActionTargetType.Target);
    }
}
