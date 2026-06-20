using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK.Action.Gcd;

public class 掠影的蔑视Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 这玩意10m啊？？？
        // 总之延后这个就是在差不多弗雷还剩15秒左右放掉呢？
        // 还是说直接最后2.5秒放掉？
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > 10f) return new CheckResult(false, "当前目标过远（>10m）");
        
        // QT控制
        // if (!PromeSettings.Instance.GetQt(DRKQt.伤残)) return new CheckResult(false, "未开启远离伤残");
        
        if (Core.Me.DistanceToMe() <= 10f && Core.Me.HasStatus(DRKBuff.掠影的蔑视预备))
        {
            if (PromeSettings.Instance.GetQt(DRKQt.延后掠影的蔑视) && JobGaugeHelper.DRK.ShadowTimeRemaining > 10) return new CheckResult(false, "可以使用但是开启了延后");
            
            return new CheckResult(true, "距离 <= 10");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var target = Core.Target;
        return new PAction(DRKSkill.掠影的蔑视, ActionType.Gcd, ActionTargetType.Target);
    }
}
