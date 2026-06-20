using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Action.OffGcd;

public class 战逃反应OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        var cd = PLDSkill.战逃反应.GetActionCooldown();
        
        // QT
        if (PromeSettings.Instance.GetQt(PLDQt.不打60)) return new CheckResult(false, "当前未开启QT");
        
        // 这里写两种情况
        // 以是否开启最优战逃来区分
        if (cd < 0.1f)
        {
            // 这个真的有必要吗？
            if (PromeSettings.Instance.GetQt(PLDQt.倾泻资源))
            {
                return new CheckResult(true, "倾泻资源");
            }
            
            if (PromeSettings.Instance.GetQt(PLDQt.最优战逃))
            {
                if (PaladinHelper.IsCanUse60())
                {
                    return new CheckResult(true, $"最优战逃 数量：{PaladinHelper.GetHighDamageCount()}");
                }
                return new CheckResult(false, $"不是最优战逃 数量：{PaladinHelper.GetHighDamageCount()}");
            }
            
            return new CheckResult(true, "CD到了");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PLDSkill.战逃反应, ActionType.OffGcd, ActionTargetType.Self);
    }
}
