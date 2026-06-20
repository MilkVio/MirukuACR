using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;

namespace MilkVio.DPS.Monk.Action.OffGcd;

public class 红莲OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        // QT控制
        if (PromeSettings.Instance.GetQt(MNKQt.不打60)) return new CheckResult(false, "不打60");
        if (PromeSettings.Instance.GetQt(MNKQt.攒资源)) return new CheckResult(false, "已开启攒资源");
        
        // 逻辑
        // 考虑是否加入对齐120的逻辑？但是似乎这里轴控就行
        if (MNKSkill.红莲极意.GetActionCooldown() == 0)
        {
            return new CheckResult(true, "所有条件满足 && 冷却完毕");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(MNKSkill.红莲极意, ActionType.OffGcd, ActionTargetType.Self);
    }
}
