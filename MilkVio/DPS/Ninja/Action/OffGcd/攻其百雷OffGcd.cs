using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.OffGcd;

public class 攻其百雷OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(3);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        var me = Core.Me;
        var hasRenyin = me.HasStatus(NinjaBuff.忍隐);
        var leftTime60 = NinjaHelper.GetNinja60ActionId().GetActionCooldown();
        
        if (hasRenyin && leftTime60 == 0)
        {
            if (!PromeSettings.Instance.GetQt(NinjaQt.不打60))
            {
                return new CheckResult(true, "cd==0");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        return new PAction(NinjaHelper.GetNinja60ActionId(), ActionType.OffGcd, ActionTargetType.Target);
    }
}
