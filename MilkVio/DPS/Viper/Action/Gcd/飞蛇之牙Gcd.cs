using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Action.Gcd;

public class 飞蛇之牙Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ViperQt.飞蛇之牙)) return new CheckResult(false, "未开启QT");
        
        var currentAttackRange = GameData.GetCurrentAttackRange(20);
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        if (Core.Me.DistanceToMe() <= currentMeleeRange) return new CheckResult(false, $"当前目标过近（>{currentAttackRange}m）");
        
        return new CheckResult(true, "远离丢牙");
    }

    public PAction GetAction()
    {
        return new PAction(ViperSkill.飞蛇之牙, ActionType.Gcd, ActionTargetType.Target);
    }
}
