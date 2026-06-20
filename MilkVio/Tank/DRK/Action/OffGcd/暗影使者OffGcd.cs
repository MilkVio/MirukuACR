using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK.Action.OffGcd;

public class 暗影使者OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 理论上暗影使者只应该在120中使用 一切特殊情况去轴控
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > 10f) return new CheckResult(false, "当前目标过远（>10m）");
        if (Core.Me.Level < 90) return new CheckResult(false, "未习得该技能");

        if (DRKSkill.暗影使者.GetActionCharges() >= 1)
        {
            // 最终爆发
            if (PromeSettings.Instance.GetQt(DRKQt.最终爆发)) return new CheckResult(true, "最终爆发");
            
            // 120中
            if (Core.Me.DistanceToMe() <= 10f && JobGaugeHelper.DRK.ShadowTimeRemaining > 0)
            {
                return new CheckResult(true, "正在爆发中");
            }
            return new CheckResult(false, $"当前不在120或最终爆发 层数：{DRKSkill.暗影使者.GetActionCharges()}");
        }
        
        return new CheckResult(false, $"未冷却 层数：{DRKSkill.暗影使者.GetActionCharges()}");
    }

    public PAction GetAction()
    {
        return new PAction(DRKSkill.暗影使者, ActionType.OffGcd, ActionTargetType.Target);
    }
}
