using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.OffGcd;

public class 分身之术OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(3);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        var ninki = JobGaugeHelper.NIN.Ninki;
        var isCanUse = NinjaSkill.分身之术.GetActionCooldown() == 0 && ninki >= 50;

        if (!PromeSettings.Instance.GetQt(NinjaQt.分身之术)) return new CheckResult(false, $"未开启分身之术QT");
        
        if (isCanUse)
        {
            if (PromeSettings.Instance.GetQt(NinjaQt.镰鼬对齐120))
            {
                if (NinjaHelper.Is120()) return new CheckResult(true, "当前正在120");
                if (NinjaSkill.介毒之术.GetActionCooldown() < 5) return new CheckResult(true, "当前正在120");
            }
            if (!PromeSettings.Instance.GetQt(NinjaQt.镰鼬对齐120))
            {
                return new CheckResult(true, "好了就用");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        return new PAction(NinjaSkill.分身之术, ActionType.OffGcd, ActionTargetType.Self);
    }
}
