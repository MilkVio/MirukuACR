using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Action.Gcd;

public class 圣环Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Me.Level < 72) return new CheckResult(false, "当前等级不足");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (!PromeSettings.Instance.GetQt(PLDQt.AOE)) return new CheckResult(false, "未开启AOE");
        
        if (TargetHelper.EnemyInRange(5) > 2 && Core.Me.HasStatus(PLDBuff.神圣魔法效果提高))
        {
            return new CheckResult(true, "5M内有3个敌人");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PLDSkill.圣环, ActionType.Gcd, ActionTargetType.Self);
    }
}
