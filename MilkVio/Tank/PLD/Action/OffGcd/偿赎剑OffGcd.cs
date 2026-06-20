using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Action.OffGcd;

public class 偿赎剑OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if(PromeSettings.Instance.GetQt(PLDQt.不打双能力技)) return new CheckResult(false, "未开启QT");
        
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, "当前目标距离过远");
        float cd = 99;
        var fofCd = PLDSkill.战逃反应.GetActionCooldown();
        if (Core.Me.Level < 86)
        {
            cd = PLDSkill.深奥之灵.GetActionCooldown();
        }
        else
        {
            cd = PLDSkill.偿赎剑.GetActionCooldown();
        }

        if (Core.Me.DistanceToMe() <= currentMeleeRange && cd == 0)
        {
            // 这个真的有必要吗？
            if (PromeSettings.Instance.GetQt(PLDQt.倾泻资源))
            {
                return new CheckResult(true, "倾泻资源");
            }
            
            // 这里以是否开启打60来区分
            // 如果开启了不打60
            // 好了就直接打掉 因为无需等战逃
            // 如果关闭了不打60
            if (PromeSettings.Instance.GetQt(PLDQt.最优战逃))
            {
                if (Core.Me.HasStatus(PLDBuff.战逃反应buff))
                {
                    return new CheckResult(true, "自身有战逃");
                }
                if (!PromeSettings.Instance.GetQt(PLDQt.不打60))
                {
                    if (fofCd <= 10)
                    {
                        return new CheckResult(false, "等待战逃生效");
                    }
                    return new CheckResult(true, "战逃时间太久了 不等了");
                }
            }
            
            return new CheckResult(true, "距离 < 3");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if (Core.Me.Level < 86)
        {
            return new PAction(PLDSkill.深奥之灵, ActionType.OffGcd, ActionTargetType.Target);
        }
        return new PAction(PLDSkill.偿赎剑, ActionType.OffGcd, ActionTargetType.Target);
    }
}
