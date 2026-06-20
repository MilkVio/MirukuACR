using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.Gcd;

public class 武器彩绘Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // QT控制
        if (!PromeSettings.Instance.GetQt(PCTQt.武器画)) return new CheckResult(false, "未开启QT");
        if (JobGaugeHelper.PCT.WeaponMotifDrawn) return new CheckResult(false, "当前拥有武器画");
        
        // 没有武器画 不在移动 符合阈值
        var player = Core.Me;
        var canUse = !PctHelper.IsWillMoveOrMoved();
        
        if (player.HasStatus(PCTBuff.星空构想)) return new CheckResult(false, "团副内不画画");
        if (player.HasStatus(PCTBuff.重锤连击)) return new CheckResult(false, "当前正在打锤子");
        /*
         * 绘画逻辑 初版
         * 如果开启上天画画:
         * 没有敌人直接画
         * 以倾泻绘画来区分
         * 未开启：
         * 锤子30秒开始画
         * 开启：
         * 直接画
         */
        if (canUse)
        {
            if (PromeSettings.Instance.GetQt(PCTQt.倾泻资源))
            {
                if (PctHelper.GetCurrentCreatureCharge() < 1)
                {
                    return new CheckResult(false, "倾斜资源 没有武器构想 不打");
                }
            }
            
            if (PromeSettings.Instance.GetQt(PCTQt.上天画画) && TargetHelper.IsAllBossUntargetable())
            {
                return new CheckResult(true, "当前正在上天画画");
            }
            
            if (PromeSettings.Instance.GetQt(PCTQt.倾泻彩绘))
            {
                if (PCTSkill.武器彩绘.GetActionCharges() >= 1)
                {
                    return new CheckResult(true, "当前正在倾泻彩绘");
                }
            }

            if (PCTSkill.武器构想.GetActionCooldown() < 30)
            {
                return new CheckResult(true, "当前武器构想CD < 30");
            }
            
            return new CheckResult(false, "可以使用 但是不满足任何条件");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.武器彩绘, ActionType.Gcd, ActionTargetType.Self);
    }
}
