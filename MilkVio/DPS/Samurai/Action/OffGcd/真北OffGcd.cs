using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Action.OffGcd;

public class 真北OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (!PromeSettings.Instance.GetQt(SAMQt.真北)) return new CheckResult(false, "未开启自动真北");
        // 前置条件
        if(Core.Me.HasStatus(1250)) return new CheckResult(false, "自身已存在真北");
        
        var myPostional = TargetHelper.GetTargetPositional();
        var needPostional = SamuraiHelper.GetNeedPositional();
        var isCanUse = ActionHelper.GetLastComboID() == SAMSkill.阵风 || ActionHelper.GetLastComboID() == SAMSkill.士风 || Core.Me.HasStatus(SAMBuff.明镜止水);
        
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
