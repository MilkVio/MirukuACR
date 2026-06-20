using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.OffGcd;

public class 减色混合OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var player = Core.Me;
        var canUse = PctHelper.GetCurrentReverseCharge() > 0 && !player.HasStatus(PCTBuff.色调反转) && !player.HasStatus(PCTBuff.减色混合);
        if (PromeSettings.Instance.GetQt(PCTQt.不打减色)) return new CheckResult(false, "已开启不打减色");
        
        // Qt控制
        // todo
        
        /* 大体逻辑↓ 初版
         * 1.倾泻资源模式：检测QT >1直接开
         * 2.团辅模式：检测自身是否有团辅buff
         * 3.正常模式：
         * 可能存在的团辅留资源逻辑？目前先写>=75颜料开
         */
        if (canUse)
        {
            if (PromeSettings.Instance.GetQt(PCTQt.倾泻减色))
            {
                return new CheckResult(true, "开启倾泻减色");
            }
            
            if (PromeSettings.Instance.GetQt(PCTQt.倾泻资源))
            {
                return new CheckResult(true, "开启倾泻资源");
            }

            if (player.HasStatus(PCTBuff.星空构想))
            {
                return new CheckResult(true, "团副内");
            }

            if (!player.HasStatus(PCTBuff.星空构想) && JobGaugeHelper.PCT.PalleteGauge > 75)
            {
                return new CheckResult(true, "> 75");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.减色混合, ActionType.OffGcd, ActionTargetType.Self);
    }
}
