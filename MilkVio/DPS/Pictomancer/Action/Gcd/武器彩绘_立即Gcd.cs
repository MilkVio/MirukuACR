using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.Gcd;

public class 武器彩绘_立即Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // QT控制
        if (JobGaugeHelper.PCT.WeaponMotifDrawn) return new CheckResult(false, "当前拥有武器画");
        if (!PromeSettings.Instance.GetQt(PCTQt.武器画)) return new CheckResult(false, "未开启武器画");
        // 没有武器画 不在移动 符合阈值
        var canUse = !PctHelper.IsWillMoveOrMoved();
        
        if (canUse)
        {
            if (Core.Me.HasStatus(PCTBuff.星空构想)) return new CheckResult(false, "团副内不画画");
            if (Core.Me.HasStatus(PCTBuff.重锤连击)) return new CheckResult(false, "当前正在打锤子");
            if (PromeSettings.Instance.GetQt(PCTQt.立即武器画))
            {
                return new CheckResult(true, "立即画画");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.武器彩绘, ActionType.Gcd, ActionTargetType.Self);
    }
}
