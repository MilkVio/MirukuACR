using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.Gcd;

public class 风景彩绘Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // QT控制
        // todo
        if (!PromeSettings.Instance.GetQt(PCTQt.风景画)) return new CheckResult(false, "未开启QT");
        if (JobGaugeHelper.PCT.LandscapeMotifDrawn) return new CheckResult(false, "当前拥有风景画");
        
        // 没有武器画 不在移动 符合阈值
        var canUse = !PctHelper.IsWillMoveOrMoved();
        var player = Core.Me;
        
        if (player.HasStatus(PCTBuff.星空构想)) return new CheckResult(false, "团副内不画画");
        
        if (canUse)
        {
            if (PromeSettings.Instance.GetQt(PCTQt.上天画画) && TargetHelper.IsAllBossUntargetable())
            {
                return new CheckResult(true, "当前正在上天画画");
            }
            
            /*if (PromeSettings.Instance.GetQt(PCTQt.倾泻彩绘))
            {
                return new CheckResult(true, "当前正在倾泻彩绘");
            }*/

            if (PCTSkill.风景构想.GetActionCooldown() < 60)
            {
                return new CheckResult(true, "风景构想CD < 30");
            }
            
            return new CheckResult(false, "可以使用 但是不满足任何条件");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.风景彩绘, ActionType.Gcd, ActionTargetType.Self);
    }
}
