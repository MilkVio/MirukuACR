using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.OffGcd;

public class 真北OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (!PromeSettings.Instance.GetQt(NinjaQt.真北)) return new CheckResult(false, "未开启自动真北");
        // 前置条件
        if(Core.Me.HasStatus(1250)) return new CheckResult(false, "自身已存在真北");
        
        // todo
        // 120或60 0<冷卻<5的時候 不允許用真北
        
        var myPostional = TargetHelper.GetTargetPositional();
        var needPostional = NinjaHelper.GetNinjaPositionAction();
        var isCanUse = ActionHelper.GetLastComboID() == NinjaSkill.绝风;
        
        if (needPostional != Positional.None && UniversalData.MeleeUniversalSkill.真北.GetActionCharges() >= 1 && isCanUse)
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
        return new PAction(UniversalData.MeleeUniversalSkill.真北, ActionType.OffGcd, ActionTargetType.Self);
    }
}
