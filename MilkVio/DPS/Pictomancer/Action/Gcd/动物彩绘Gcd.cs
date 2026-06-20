using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.Gcd;

public class 生物彩绘Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // QT控制
        // todo
        if (Core.Me == null) return new CheckResult(false, "自己不存在");
        if (!PromeSettings.Instance.GetQt(PCTQt.动物画)) return new CheckResult(false, "未开启QT");
        if (JobGaugeHelper.PCT.CreatureMotifDrawn) return new CheckResult(false, "当前拥有动物画");
        
        var canUse = !PctHelper.IsWillMoveOrMoved();
        var player = Core.Me;

        if (player.HasStatus(PCTBuff.星空构想)) return new CheckResult(false, "团副内不画画");
        
        if (canUse)
        {
            if (PromeSettings.Instance.GetQt(PCTQt.倾泻资源))
            {
                if (PctHelper.GetCurrentCreatureCharge() < 1)
                {
                    return new CheckResult(false, "倾斜资源 没有动物构想 不打");
                }
            }
            
            if (PromeSettings.Instance.GetQt(PCTQt.上天画画) && TargetHelper.IsAllBossUntargetable())
            {
                return new CheckResult(true, "当前正在上天画画");
            }
            
            if (PromeSettings.Instance.GetQt(PCTQt.倾泻彩绘))
            {
                if (PctHelper.GetCurrentCreatureCharge() >= 1)
                {
                    return new CheckResult(true, "当前正在倾泻彩绘");
                }
            }

            if (PctHelper.GetCurrentCreatureCharge() > 0.5) 
            {
                return new CheckResult(true, "当前动物构想 > 0.5");
            }
            
            return new CheckResult(false, "可以使用 但是不满足任何条件");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.动物彩绘, ActionType.Gcd, ActionTargetType.Self);
    }
}
