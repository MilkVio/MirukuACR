using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Action.OffGcd;

public class 双牙术OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        var currentAttackRange = GameData.GetCurrentAttackRange(20);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        var me = Core.Me;
        var distanceToMe = Core.Target.DistanceToMe();
        
        if (me.HasStatus(ViperBuff.乱击双牙) || me.HasStatus(ViperBuff.连击双牙))
        {
            if (distanceToMe <= currentMeleeRange)
                return new CheckResult(true, "近战范围内，且有双牙buff");
        }

        if (me.HasStatus(ViperBuff.乱尾锐尾) || me.HasStatus(ViperBuff.连尾锐尾))
        {
            if (distanceToMe <= currentAttackRange)
            {
                return new CheckResult(true, "技能变化");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var me = Core.Me;
        if(me.HasStatus(ViperBuff.乱击双牙))
            return new PAction(ViperSkill.双牙乱击, ActionType.OffGcd, ActionTargetType.Target);
        if(me.HasStatus(ViperBuff.连击双牙))
            return new PAction(ViperSkill.双牙连击, ActionType.OffGcd, ActionTargetType.Target);
        if(me.HasStatus(ViperBuff.乱尾锐尾))
            return new PAction(ViperSkill.飞蛇乱尾击, ActionType.OffGcd, ActionTargetType.Target);
        return new PAction(ViperSkill.飞蛇连尾击.GetAdjustedActionId(), ActionType.OffGcd, ActionTargetType.Target);
    }
}
