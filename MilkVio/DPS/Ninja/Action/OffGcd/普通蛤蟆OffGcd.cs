using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.OffGcd;

public class 普通蛤蟆OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(3);
        if (!PromeSettings.Instance.GetQt(NinjaQt.AOE)) return new CheckResult(false, "当前未开启QT");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        if (Core.Me.HasStatus(NinjaBuff.秘技预备)) return new CheckResult(false, "当前有秘技");
        var ninki = JobGaugeHelper.NIN.Ninki;
        var isCanUseAoe = (Core.Me.HasStatus(NinjaBuff.命水) && TargetHelper.EnemyIn5m() >= 3) ||
                          (!Core.Me.HasStatus(NinjaBuff.命水) && TargetHelper.EnemyIn5m() >= 2);
        
        if (ninki >= 50 && isCanUseAoe)
        {
            if (ninki >= 80)
            {
                return new CheckResult(true, "打一个");
            }
            
            if (ninki >= 60 && NinjaSkill.介毒之术.GetActionCooldown() < 5)
            {
                return new CheckResult(true, "团辅前泄一个");
            }
            
            if (PromeSettings.Instance.GetQt(NinjaQt.倾泻资源))
            {
                return new CheckResult(true, "倾泻");
            }
            
            if (NinjaHelper.Is60() && NinjaSkill.介毒之术.GetActionCooldown() > 35f)
            {
                return new CheckResult(true, "60爆发泄");
            }
            
            if (NinjaHelper.Is60() && NinjaHelper.Is120())
            {
                return new CheckResult(true, "团辅泄");
            }
            
            if (NinjaHelper.Is120() && NinjaHelper.GetNinja60ActionId().GetActionCooldown() > 35f)
            {
                return new CheckResult(true, "团辅对不齐泄");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        return new PAction(NinjaSkill.大虾蟆, ActionType.OffGcd, ActionTargetType.Self);
    }
}
