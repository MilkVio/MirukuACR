using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;


namespace MilkVio.DPS.Monk.Action.Gcd;

public class 绝空拳Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = MonkHelper.GetCurrentMeleeRange();
        var CurrentAttackRange = MonkHelper.GetCurrentAttackRange(10f);
        

        var buffLeftTime = Core.Me.GetStatusLeftTime(MNKBuff.绝空拳预备);

        // 一些条件
        var hasFire = Core.Me.HasStatus(MNKBuff.乾坤斗气弹预备) ? 2 : 0;
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var targetDistanceToMe = Core.Me.DistanceToMe();
        if (Core.Me.DistanceToMe() > CurrentAttackRange) return new CheckResult(false, $"当前目标过远（>{CurrentAttackRange}m）");
        if (Core.Me.Level < 96) return new CheckResult(false, "当前等级不够");
        
        // 必要自身条件
        if (!Core.Me.HasStatus(MNKBuff.绝空拳预备)) return new CheckResult(false, "当前无法发动乾坤斗气弹");
        
        // 逻辑 QT+条件混合逻辑
        // 大体分两种情况 近战距离打得到 || 近战距离打不到
        
        // 近战距离打得到
        if (targetDistanceToMe <= CurrentMeleeRange)
        {
            if (!PromeSettings.Instance.GetQt(MNKQt.延后绝空拳)) return new CheckResult(true, $"未开启延后 检测到Buff");
            if (PromeSettings.Instance.GetQt(MNKQt.延后绝空拳) && buffLeftTime + hasFire < 2.5f)
            {
                return new CheckResult(true, $"buff即将超时 直接使用");
            }
            
            return new CheckResult(false, "近战距离 但是不满足任何条件");
        }
        
        // 近战距离打不到
        if (targetDistanceToMe > CurrentMeleeRange)
        {
            return new CheckResult(true, $"近战距离打不到");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(MNKSkill.绝空拳, ActionType.Gcd, ActionTargetType.Target);
    }
}
