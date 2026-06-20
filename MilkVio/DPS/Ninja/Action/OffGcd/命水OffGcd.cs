using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.OffGcd;

// 12060才按 忍气需要<50
public class 命水OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        var cd = NinjaSkill.命水.GetActionCooldown();
        var status = Core.Me.HasStatus(NinjaBuff.忍隐);
        var ninki = JobGaugeHelper.NIN.Ninki;
        var isCanUse = cd == 0 && status && ninki <= 50;
        
        // QT控制
        
        if (isCanUse)
        {
            if (NinjaHelper.Is60() && NinjaHelper.Is120())
            {
                return new CheckResult(true, "cd==0");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        return new PAction(NinjaSkill.命水, ActionType.OffGcd, ActionTargetType.Self);
    }
}
