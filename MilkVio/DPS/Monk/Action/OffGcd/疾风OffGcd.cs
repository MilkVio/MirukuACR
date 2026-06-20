using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;

namespace MilkVio.DPS.Monk.Action.OffGcd;

public class 疾风OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.Level < 72) return new CheckResult(false, "当前等级不够");
        // QT控制
        if (PromeSettings.Instance.GetQt(MNKQt.攒资源)) return new CheckResult(false, "已开启攒资源");
        
        if (MNKSkill.疾风极意.GetActionCooldown() == 0)
        {
            if (PromeSettings.Instance.GetQt(MNKQt.疾风对齐120))
            {
                if(MonkHelper.IsIn120()) return new CheckResult(true, "正在120 && 冷却完毕");
                return new CheckResult(false, "开启对齐120 && 未到达120");
            }
            
            return new CheckResult(true, "所有条件满足 && 冷却完毕");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(MNKSkill.疾风极意, ActionType.OffGcd, ActionTargetType.Self);
    }
}
