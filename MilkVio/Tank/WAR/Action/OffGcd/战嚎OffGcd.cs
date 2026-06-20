using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.WAR.WARData;

namespace MilkVio.Tank.WAR.Action.OffGcd;

public class 战嚎OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        // Qt控制
        if (PromeSettings.Instance.GetQt(WARQt.攒资源)) return new CheckResult(false, "已开启攒资源");
        if (Core.Me.HasStatus(WARBuff.原初的混沌)) return new CheckResult(false, "自身已有战嚎");
        var actionCd = WARSkill.战嚎.GetActionCooldown();
        
        // 逻辑主体
        if (WARSkill.战嚎.GetActionCharges() >= 1)
        {
            if (Core.Me.HasStatus(WARBuff.原初的觉悟))
            {
                var libStack = Core.Me.GetStatusStackCount(WARBuff.原初的解放);
                var libLeftTime = Core.Me.GetStatusLeftTime(WARBuff.原初的解放);
                // 这里写原初的解放层数逻辑 要在能打掉原初解放的前提下再按
                if (libStack == 0 && libLeftTime == 0 && JobGaugeHelper.WAR.Beast <= 50)
                {
                    return new CheckResult(true, "解放内可以打一个");
                }
                if (libStack * 2.5f + 5f < libLeftTime && JobGaugeHelper.WAR.Beast <= 50)
                {
                    return new CheckResult(true, "解放内可以打一个");
                }
            }
            else if (actionCd <= 20 && JobGaugeHelper.WAR.Beast <= 50)
            {
                return new CheckResult(true, "平常打一个");
            }

            if (PromeSettings.Instance.GetQt(WARQt.最终爆发) && JobGaugeHelper.WAR.Beast <= 50) return new CheckResult(true, "最终爆发");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(WARSkill.战嚎, ActionType.OffGcd, ActionTargetType.Self);
    }
}
