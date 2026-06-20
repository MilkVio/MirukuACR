using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.OffGcd;

public class 坏死爆发OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        if(!PromeSettings.Instance.GetQt(SMNQt.豆子)) return new CheckResult(false, "未开启豆子");
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        if (me.Level >= 92)
        {
            if (SMNSkill.坏死爆发.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        }
        else
        {
            if (SMNSkill.溃烂爆发.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        }

        if (JobGaugeHelper.SMN.AetherflowStacks >= 1)
        {
            if (PromeSettings.Instance.GetQt(SMNQt.倾泻资源))
            {
                return new CheckResult(true, "倾泻资源");
            }
            
            if (PromeSettings.Instance.GetQt(SMNQt.不打120))
            {
                if (SMNSkill.能量吸收.GetActionCooldown() != 0)
                {
                    return new CheckResult(true, "未开启120 技能好了泄资源");
                }
            }

            if (me.HasStatus(SMNBuff.灼热之光))
            {
                return new CheckResult(true, "团副内泄资源");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if (Core.Me.Level >= 92)
        {
            return new PAction(SMNSkill.坏死爆发, ActionType.OffGcd, ActionTargetType.Target);
        }
        return new PAction(SMNSkill.溃烂爆发, ActionType.OffGcd, ActionTargetType.Target);
    }
}
