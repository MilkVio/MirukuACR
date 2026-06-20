using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.OffGcd;

public class 秘技蛤蟆OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(3);
        if (!PromeSettings.Instance.GetQt(NinjaQt.AOE)) return new CheckResult(false, "当前未开启QT");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        var ninki = JobGaugeHelper.NIN.Ninki;
        var isCanUse = Core.Me.HasStatus(NinjaBuff.秘技预备) && ninki >= 50;
        var isCanUseAoe = (Core.Me.HasStatus(NinjaBuff.命水) && TargetHelper.EnemyIn5m() >= 3) ||
                          (!Core.Me.HasStatus(NinjaBuff.命水) && TargetHelper.EnemyIn5m() >= 2);
        
        if (isCanUse && isCanUseAoe)
        {
            if (NinjaHelper.Is60() && NinjaHelper.Is120())
            {
                return new CheckResult(true, "1");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        return new PAction(NinjaSkill.虾蟆仙, ActionType.OffGcd, ActionTargetType.Self);
    }
}
