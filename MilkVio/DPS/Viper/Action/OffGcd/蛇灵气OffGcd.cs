using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Action.OffGcd;

public class 蛇灵气OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if(!PromeSettings.Instance.GetQt(ViperQt.蛇灵气)) return new CheckResult(false, "未开启QT");
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");

        if (Core.Me.Level < 86) return new CheckResult(false, "等级不足");
        if (ViperHelper.IsIn强碎灵()) return new CheckResult(false, "当前在强碎灵");
        
        var cd = ViperSkill.蛇灵气.GetActionCooldown() == 0;
        return new CheckResult(cd, $"技能冷却 {cd}");
    }

    public PAction GetAction()
    {
        return new PAction(ViperSkill.蛇灵气.GetAdjustedActionId(), ActionType.OffGcd, ActionTargetType.Self);
    }
}
