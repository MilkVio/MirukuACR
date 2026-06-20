using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.Gcd;

public class AOEGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(NinjaQt.AOE)) return new CheckResult(false, "当前未开启QT");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (NinjaHelper.Is结印()) return new CheckResult(false, "正在结印/月影");
        
        if (TargetHelper.EnemyIn5m() >= 3)
        {
            return new CheckResult(true, $"敌人足够");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var lastActionId = ActionHelper.GetLastComboID();
        if (lastActionId == NinjaSkill.血雨飞花) return new PAction(NinjaSkill.八卦无刃杀, ActionType.Gcd, ActionTargetType.Self);
        return new PAction(NinjaSkill.血雨飞花, ActionType.Gcd, ActionTargetType.Self);
    }
}
