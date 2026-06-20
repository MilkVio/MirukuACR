using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Action.Gcd;

public class 明镜Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.DistanceToMe() > CurrentMeleeRange) return new CheckResult(false, $"当前目标过远（>{CurrentMeleeRange}m）");
        var isCanUse = Core.Me.HasStatus(SAMBuff.明镜止水);
        
        if (Core.Me.DistanceToMe() <= CurrentMeleeRange && isCanUse)
        {
            return new CheckResult(true, $"{CurrentMeleeRange}");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        var type = SamuraiHelper.GetBestMingJingType();
        switch (type)
        {
            case Combo类型.月:
                return new PAction(SAMSkill.月光, ActionType.Gcd, ActionTargetType.Target);
            case Combo类型.花:
                return new PAction(SAMSkill.花车, ActionType.Gcd, ActionTargetType.Target);
            case Combo类型.雪:
                return new PAction(SAMSkill.雪风, ActionType.Gcd, ActionTargetType.Target);
        }

        return null;
    }
}
