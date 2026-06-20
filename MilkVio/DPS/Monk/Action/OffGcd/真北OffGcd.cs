using ECommons.DalamudServices;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;

namespace MilkVio.DPS.Monk.Action.OffGcd;

public class 真北OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(MNKQt.真北)) return new CheckResult(false, "当前无目标");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (UniversalData.MeleeUniversalSkill.真北.GetActionCharges() < 1) return new CheckResult(false, "真北数量不足");
        
        var hasPb = Core.Me.HasStatus(MNKBuff.震脚);
        var hasCoe = Core.Me.HasStatus(MNKBuff.猛豹身形);
        var isCanUse = ActionHelper.GetGcdRemain() < 0.7f;
        var pos = TargetHelper.GetTargetPositional();
        var coeCurrentPos = JobGaugeHelper.MNK.CoeurlFury > 0 ? Positional.Flank : Positional.Rear;
        var needUse = pos != coeCurrentPos;
        
        // 不符合的自身条件 有真北 懒得写了 之后再写
        if (Core.Me.HasStatus(1250)) return new CheckResult(false, "已有真北");
        
        // QT控制
        
        // 逻辑
        // 大体分为两种情况 有震脚 无震脚
        // 无震脚 检测上一个当前是否持有 猛豹 
        // 有震脚 需要拓展helper方法获取是否在打阳
        if (isCanUse)
        {
            if (!hasPb && hasCoe && needUse)
            {
                return new CheckResult(true, "需要按一个");
            }

            if (hasPb && MonkHelper.GetNextNadi() == NadiType.阳)
            {
                if (MonkHelper.AutoGetSolarBeastType() == BeastType.Coe && needUse)
                {
                    return new CheckResult(true, "所有条件满足 && 冷却完毕");
                }
            }
        }
        
        
        return new CheckResult(false, "不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(UniversalData.MeleeUniversalSkill.真北, ActionType.OffGcd, ActionTargetType.Self);
    }
}
