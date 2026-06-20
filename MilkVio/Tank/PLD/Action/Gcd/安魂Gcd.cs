using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Action.Gcd;

public class 安魂Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.Level < 80) return new CheckResult(false, "等级不足");
        if (Core.Me.CurrentMp < 1000) return new CheckResult(false, "蓝量不够");
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, "当前目标距离过远");
        
        if (Core.Me.DistanceToMe() <= currentAttackRange)
        {
            if (PaladinHelper.HasBlade())
            {
                if (PromeSettings.Instance.GetQt(PLDQt.延后大宝剑))
                {
                    if (PaladinHelper.IsBladeInLimitTime())
                    {
                        return new CheckResult(true, "极限时间");
                    }
                    
                    return new CheckResult(false, "当前正在延后大宝剑");
                }
                
                return new CheckResult(true, "距离 < 3");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if (Core.Me.Level < 90)
        {
            return new PAction(PLDSkill.悔罪, ActionType.Gcd, ActionTargetType.Target);
        }
        return PaladinHelper.GetAdjusteBlade();
    }
}
