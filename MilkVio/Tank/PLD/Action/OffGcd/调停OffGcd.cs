using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Action.OffGcd;

public class 调停OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > 3) return new CheckResult(false, "当前目标距离过远");
        var targetRange = Core.Me.DistanceToMe();
        var charge = PLDSkill.调停.GetActionCharges();
        var has战逃 = Core.Me.HasStatus(PLDBuff.战逃反应buff);
        
        // 距离判断
        if (targetRange > 25) return new CheckResult(false, "距离过远");
        // QT
        if (!PromeSettings.Instance.GetQt(PLDQt.调停)) return new CheckResult(false, "未开启QT");
        
        if (charge > 1.9f)
        {
            return new CheckResult(true, "泄一个");
        }

        if (has战逃 && charge >= 1f)
        {
            return new CheckResult(true, "战逃");
        }
        
        if (PromeSettings.Instance.GetQt(PLDQt.倾泻资源) && charge >= 1f)
        {
            return new CheckResult(true, "倾泻资源");
        }
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PLDSkill.调停, ActionType.OffGcd, ActionTargetType.Target);
    }
}
