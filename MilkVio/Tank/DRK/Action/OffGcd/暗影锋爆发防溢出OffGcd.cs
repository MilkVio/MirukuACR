using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK.Action.OffGcd;

public class 暗影锋爆发防溢出OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 基础判断
        // 主要为了起手的时候，防止暗影峰没有被按 从而溢出蓝量
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() >= 3f) return new CheckResult(false, "当前目标过远（>3m）");
        
        // QT控制
        if (PromeSettings.Instance.GetQt(DRKQt.保留3000蓝) && Core.Me.CurrentMp < 6000) return new CheckResult(false, "已保留3000蓝");
        
        // 第一种情况 例如转场P5 没有轴控的情况 => 检测是否有60秒技能 120秒技能 从而判断是否需要使用
        var hasMPAction = DRKSkill.血乱.GetActionCooldown() == 0 || DRKSkill.精雕怒斩.GetActionCooldown() == 0 ||
                          DRKSkill.掠影示现.GetActionCooldown() == 0;

        if (hasMPAction && Core.Me.CurrentMp > 9000)
        {
            return new CheckResult(true, "情况1 爆发前蓝量即将溢出");
        }
        
        // 第二种情况 爆发中 蓝量即将溢出 但是由于队列特性无法直接使用 so
        if (JobGaugeHelper.DRK.ShadowTimeRemaining > 0 && Core.Me.CurrentMp > 9000)
        {
            return new CheckResult(true, "情况2 爆发中蓝量即将溢出");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if (Core.Me.Level < 74) return new PAction(DRKSkill.暗黑锋, ActionType.OffGcd, ActionTargetType.Target);
        return new PAction(DRKSkill.暗影锋, ActionType.OffGcd, ActionTargetType.Target);
    }
}
