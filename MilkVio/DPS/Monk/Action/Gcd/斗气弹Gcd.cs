using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;


namespace MilkVio.DPS.Monk.Action.Gcd;

public class 斗气弹Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = MonkHelper.GetCurrentMeleeRange();
        var CurrentAttackRange = MonkHelper.GetCurrentAttackRange(20f);

        // 一些条件
        var hasFs = Core.Me.HasStatus(MNKBuff.演武);
        var hasPb = Core.Me.HasStatus(MNKBuff.震脚);
        var hasMb = JobGaugeHelper.MNK.BlitzTimeRemaining > 0;
        var hasOpo = Core.Me.HasStatus(MNKBuff.魔猿身形);
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > CurrentAttackRange) return new CheckResult(false, $"当前目标过远（>{CurrentAttackRange}m）");
        if (Core.Me.Level < 100) return new CheckResult(false, "当前等级不够");
        
        // 必要自身条件
        if (!Core.Me.HasStatus(MNKBuff.乾坤斗气弹预备)) return new CheckResult(false, "当前无法发动乾坤斗气弹");
        var buffLeftTime = Core.Me.GetStatusLeftTime(MNKBuff.乾坤斗气弹预备);
        
        // QT控制 这个似乎不需要QT控制
        // if (!PromeSettings.Instance.GetQt(MNKQt.必杀技)) return new CheckResult(false, "已关闭必杀技");
        
        // 逻辑
        // 大体分两种情况 近战距离打得到 || 近战距离打不到
        // 近战距离打得到后粗分两种情况
        // 1.保留3G（14s） 防止出现远离情况 之前不打
        // 2.14S以内 如果自身没有震脚 没有必杀技 没有演武放一个
        // 3.如果存在上述任何情况 但是时间<2.5s，直接释放
        
        // 近战距离打得到
        if (Core.Me.DistanceToMe() <= CurrentMeleeRange)
        {
            if (buffLeftTime < 2.5f) return new CheckResult(true, $"Buff即将消失 打掉");
            
            if (buffLeftTime <= 14)
            {
                if (!hasFs && !hasPb && !hasMb && !hasOpo) 
                {
                    return new CheckResult(true, $"没有任何能打魔猿的Buff 打掉");
                }
            }

            if (PromeSettings.Instance.GetQt(MNKQt.最终爆发) || PromeSettings.Instance.GetQt(MNKQt.倾泻资源))
            {
                return new CheckResult(true, $"倾泻");
            }
            
            return new CheckResult(false, "近战距离 但是不满足任何条件");
        }
        
        // 近战距离打不到
        if (Core.Me.DistanceToMe() > CurrentMeleeRange)
        {
            return new CheckResult(true, $"近战距离打不到");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(MNKSkill.乾坤斗气弹, ActionType.Gcd, ActionTargetType.Target);
    }
}
