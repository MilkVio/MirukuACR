using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;
using MilkVio.DPS.Samurai.Level100;
using MilkVio.DPS.Samurai.Timeline;

namespace MilkVio.DPS.Samurai.Action.OffGcd;

public class 明镜止水OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Samurai100Helper.Enabled && SamuraiTimeline.MirrorPending)
        {
            if (Core.Me == null || Core.Me.IsDead) return new CheckResult(false, "死亡或角色不可用时保留请求");
            var requested = Samurai100Helper.ShouldUseMeikyo(out var requestReason);
            return new CheckResult(requested, requestReason);
        }
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Samurai100Helper.Enabled)
        {
            var use = Samurai100Helper.ShouldUseMeikyo(out var reason);
            return new CheckResult(use, reason);
        }
        // 有一层 自身没有明镜buff
        var charge = SamuraiHelper.明镜止水层数();
        var isCanUse = charge >= 1f && !Core.Me.HasStatus(SAMBuff.明镜止水);
        var has花 = JobGaugeHelper.SAM.HasHana;
        var has月 = JobGaugeHelper.SAM.HasMoon;
        var has雪 = JobGaugeHelper.SAM.HasYuki;
        
        if (PromeSettings.Instance.GetQt(SAMQt.明镜止水) && isCanUse && SamuraiHelper.IsComboEnd())
        {
            if (PromeSettings.Instance.GetQt(SAMQt.倾泻资源))
            {
                return new CheckResult(true, $"倾泻资源");
            }
            if (!PromeSettings.Instance.GetQt(SAMQt.倾泻资源))
            {
                if (GameData.IsIn120() || SamuraiHelper.IsInSelf120())
                {
                    if (SAMSkill.意气冲天.GetActionCooldown() < 105 && SAMSkill.意气冲天.GetActionCooldown() != 0 && has花 && has月)
                    {
                        return new CheckResult(false, $"120倾泻 但时间剩余不多 不强制打雪");
                    }
                    return new CheckResult(true, $"120倾泻");
                }

                if (charge >= 1.5)
                {
                    if (has雪 && has月 && has花)
                    {
                        return new CheckResult(false, $"当前有雪月花");
                    }
                    
                    if (has雪 && ((has月 && !has花) || (!has月 && has花)))
                    {
                        return new CheckResult(true, $"雪+另一闪");
                    }
                    
                    if (has雪 && !has月 && !has花)
                    {
                        return new CheckResult(true, $"只有雪");
                    }
                }
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SAMSkill.明镜止水, ActionType.OffGcd, ActionTargetType.Self);
    }
}
