using Dalamud.Game.ClientState.JobGauge.Enums;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK.Action.Gcd;

public class 暗血技Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() >= GameData.GetCurrentMeleeRange()) return new CheckResult(false, "当前目标过远（>3m）");
        
        var has血乱 = Core.Me.HasStatus(DRKBuff.血乱) || Core.Me.HasStatus(DRKBuff.血乱90);

        var 即将进入血乱 = DRKSkill.血乱.GetActionCooldown() < 4f;
        
        if (Core.Me.DistanceToMe() <= GameData.GetCurrentMeleeRange())
        {
            if (has血乱) return new CheckResult(true, "有血乱buff");
            
            // 爆发QT
            if (JobGaugeHelper.DRK.Blood >= 50 && PromeSettings.Instance.GetQt(DRKQt.最终爆发)) return new CheckResult(true, "最终爆发");
            if (JobGaugeHelper.DRK.Blood >= 50 && PromeSettings.Instance.GetQt(DRKQt.倾泻资源)) return new CheckResult(true, "倾泻资源");
            
            // 
            if (即将进入血乱 && JobGaugeHelper.DRK.Blood > 70) return new CheckResult(true, "即将进入血乱 防溢出 打一个");
            
            
            // 加入通用逻辑 存在团辅 也清空暗血
            
            // 80-100爆发逻辑 清空
            if (JobGaugeHelper.DRK.Blood >= 50 && JobGaugeHelper.DRK.ShadowTimeRemaining > 0)
            {
                return new CheckResult(true, "爆发清空");
            }
            
            // 平常逻辑1 未检测到嗜血
            if (JobGaugeHelper.DRK.Blood > 60)
            {
                return new CheckResult(true, "暗血 > 60");
            }
            
            return new CheckResult(false, $"距离 < 3 但是其他条件不满足 {Core.Me.HasStatus(DRKBuff.血乱)}");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if (Core.Me.Level >= 94)
        {
            if (JobGaugeHelper.DRK.DeliriumComboStep == DeliriumStep.ScarletDelirium &&
                Core.Me.HasStatus(DRKBuff.血乱))
            {
                return new PAction(DRKSkill.血红乱, ActionType.Gcd, ActionTargetType.Target);
            }
            if(JobGaugeHelper.DRK.DeliriumComboStep == DeliriumStep.Comeuppance &&
                    Core.Me.HasStatus(DRKBuff.血乱))
            {
                return new PAction(DRKSkill.报应, ActionType.Gcd, ActionTargetType.Target);
            }
            if(JobGaugeHelper.DRK.DeliriumComboStep == DeliriumStep.Torcleaver &&
               Core.Me.HasStatus(DRKBuff.血乱))
            {
                return new PAction(DRKSkill.戮山, ActionType.Gcd, ActionTargetType.Target);
            }
        }
        
        return new PAction(DRKSkill.血溅, ActionType.Gcd, ActionTargetType.Target);
        
    }
}
