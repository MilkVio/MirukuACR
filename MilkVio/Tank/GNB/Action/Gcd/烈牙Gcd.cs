using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.GNB.GNBData;

namespace MilkVio.Tank.GNB.Action.Gcd;

public class 烈牙Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未初始化");

        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (me.DistanceToMe() <= currentMeleeRange)
        {
            if (GunbreakerHelper.GetLieFangStack() > 0.98f && JobGaugeHelper.GNB.Ammo > 0)
            {
                return new CheckResult(true, "当前子弹>0 同时 烈牙冷却完毕");
            }
            
            if (JobGaugeHelper.GNB.AmmoComboStep > 0 && JobGaugeHelper.GNB.AmmoComboStep < 3)
            {
                return new CheckResult(true, "当前进入烈牙状态");
            }
            
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        if (JobGaugeHelper.GNB.AmmoComboStep == 1)
        {
            return new PAction(GNBSkill.猛兽爪, ActionType.Gcd, ActionTargetType.Target);
        }
        
        if (JobGaugeHelper.GNB.AmmoComboStep == 2)
        {
            return new PAction(GNBSkill.凶禽爪, ActionType.Gcd, ActionTargetType.Target);
        }
        
        return new PAction(GNBSkill.烈牙, ActionType.Gcd, ActionTargetType.Target);
    }
}
