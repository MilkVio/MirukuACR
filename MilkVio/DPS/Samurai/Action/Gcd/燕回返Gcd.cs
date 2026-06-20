using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Action.Gcd;

public class 燕回返Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(6);
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        var isCanUse = SamuraiHelper.Has燕回返();
        var leftTime = SamuraiHelper.燕回返LeftTime();
        
        if (isCanUse)
        {
            // QT模式
            if (PromeSettings.Instance.GetQt(SAMQt.立即回返))
            {
                return new CheckResult(true, "立即回返 打掉");
            }
            
            // 倾泻资源模式
            if (PromeSettings.Instance.GetQt(SAMQt.倾泻资源) || GameData.IsIn120() || SamuraiHelper.IsInSelf120())
            {
                return new CheckResult(true, "倾泻资源 打掉");
            }
            
            // 极限时间
            if (leftTime < 7.5)
            {
                return new CheckResult(true, "时间要到了 打掉");
            }
            
            // 远离打掉
            if (Core.Me.DistanceToMe() > currentMeleeRange && Core.Me.DistanceToMe() <= currentAttackRange)
            {
                return new CheckResult(true, "远离 打掉");
            }
            
            // 正常逻辑
            if (SamuraiHelper.GetBestJuhe() == 居合类型.雪月花 || SamuraiHelper.GetBestJuhe() == 居合类型.天下五剑)
            {
                return new CheckResult(true, "要打居合 先打掉");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SAMSkill.燕回返, ActionType.Gcd, ActionTargetType.Target);
    }
}
