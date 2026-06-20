using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.Gcd;

public class 风景彩绘_立即Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // QT控制
        if (JobGaugeHelper.PCT.LandscapeMotifDrawn) return new CheckResult(false, "当前拥有风景画");
        if (!PromeSettings.Instance.GetQt(PCTQt.风景画)) return new CheckResult(false, "未开启风景画");
        
        // 没有武器画 不在移动 符合阈值
        var canUse = !PctHelper.IsWillMoveOrMoved();
        
        if (canUse)
        {
            if (Core.Me.HasStatus(PCTBuff.星空构想)) return new CheckResult(false, "团副内不画画");
            if (PromeSettings.Instance.GetQt(PCTQt.立即风景画))
            {
                return new CheckResult(true, "立即画画");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.风景彩绘, ActionType.Gcd, ActionTargetType.Self);
    }
}
