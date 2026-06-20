using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon.Action.OffGcd;

public class 真北OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (!PromeSettings.Instance.GetQt(DRGQt.真北)) return new CheckResult(false, "未开启自动真北");
        // 前置条件
        if(Core.Me.HasStatus(1250)) return new CheckResult(false, "自身已存在真北");
        
        var myPostional = TargetHelper.GetTargetPositional();
        var needPostional = Positional.None;
        
        // 上一个是苍穹/直刺 下一个需要侧身
        if (ActionHelper.GetLastComboID() == DragoonHelper.GetCurrentStraightComboActionId(3))
        {
            needPostional = Positional.Flank;
        }
        // 上一个是开膛枪/螺旋击 樱花怒放 樱花缭乱
        if (ActionHelper.GetLastComboID() == DragoonHelper.GetCurrentSakuraComboActionId(2) || ActionHelper.GetLastComboID() == DragoonHelper.GetCurrentSakuraComboActionId(3))
        {
            needPostional = Positional.Rear;
        }
        
        if (needPostional != Positional.None && UniversalData.MeleeUniversalSkill.真北.GetActionCharges() >= 1)
        {
            if (needPostional != myPostional && ActionHelper.GetGcdRemain() < 1f)
            {
                return new CheckResult(true, "打个身位");
            }
        }
        return new CheckResult(false, "不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(UniversalData.MeleeUniversalSkill.真北, ActionType.OffGcd, ActionTargetType.Target);
    }
}
