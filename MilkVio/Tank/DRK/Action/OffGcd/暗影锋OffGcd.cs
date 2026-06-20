using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK.Action.OffGcd;

public class 暗影锋OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 基础判断
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() >= GameData.GetCurrentMeleeRange()) return new CheckResult(false, "当前目标过远（>3m）");
        // 如果开启 已保留3000蓝
        if (PromeSettings.Instance.GetQt(DRKQt.保留3000蓝) && Core.Me.CurrentMp < 6000) return new CheckResult(false, "已保留3000蓝");
        
        // 满足条件后 进入里面QT判断
        if (Core.Me.DistanceToMe() <= GameData.GetCurrentMeleeRange() && Core.Me.CurrentMp >= 3000)
        {
            
            // 爆发QT
            // todo
            // wtf为什么这里有暗血？？
            if (JobGaugeHelper.DRK.Blood >= 50 && PromeSettings.Instance.GetQt(DRKQt.最终爆发)) return new CheckResult(true, "最终爆发");
            if (JobGaugeHelper.DRK.Blood >= 50 && PromeSettings.Instance.GetQt(DRKQt.倾泻资源)) return new CheckResult(true, "倾泻资源");
            
            // 加入通用逻辑 存在团辅 清空MP
            
            
            // 80-100爆发内逻辑 清空
            if (JobGaugeHelper.DRK.ShadowTimeRemaining > 0) return new CheckResult(true, "当前在爆发状态 清空MP");
            
            // 70级爆发内逻辑 清空
            
            
            // 平常逻辑1 保持 暗黑状态>30
            if (JobGaugeHelper.DRK.DarksideTimeRemaining < 20) return new CheckResult(true, "当前暗黑状态<30s 打一个");
            
            // 平常逻辑2 蓝量快溢出了
            if (Core.Me.CurrentMp > 8400) return new CheckResult(true, "蓝太多了 打一个");
            
            // 平常逻辑3 开嗜血了
            if (Core.Me.HasStatus(DRKBuff.嗜血) && Core.Me.CurrentMp > 6000) return new CheckResult(true, "嗜血降低暗影峰阈值");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if (Core.Me.Level < 74) return new PAction(DRKSkill.暗黑锋, ActionType.OffGcd, ActionTargetType.Target);
        return new PAction(DRKSkill.暗影锋, ActionType.OffGcd, ActionTargetType.Target);
    }
}
