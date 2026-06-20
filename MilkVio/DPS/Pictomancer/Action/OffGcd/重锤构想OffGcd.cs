using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.OffGcd;

public class 重锤构想OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var player = Core.Me;
        var canUse = !player.HasStatus(PCTBuff.重锤连击) && PCTSkill.武器构想.GetActionCharges() >= 1;

        if (player.Level < 86)
        {
            canUse = !player.HasStatus(PCTBuff.重锤连击) && PCTSkill.武器构想.GetActionCooldown() == 0;
        }
        
        // Qt控制
        if (!PromeSettings.Instance.GetQt(PCTQt.重锤构想)) return new CheckResult(false, "未开启QT");
        
        /* 大体逻辑↓ 初版
         * 1.Qt控制开关
         * 2.好了就用
         */
        // 如果武器画好了
        if (JobGaugeHelper.PCT.WeaponMotifDrawn && canUse)
        {
            if (!PromeSettings.Instance.GetQt(PCTQt.倾泻资源))
            {
                // 留锤子进爆发
                if (PCTSkill.风景构想.GetActionCooldown() < 8)
                {
                    return new CheckResult(true, "团辅前一定要打一个");
                }
                
                // 两层的逻辑
                if (player.Level >= 86)
                {
                    // 此值60为一个锤子的冷却CD
                    // 风景24 锤子18
                    if (PCTSkill.风景构想.GetActionCooldown() <= 60)
                    {
                        if (PCTSkill.武器构想.GetActionCooldown() <= 55 && PCTSkill.风景构想.GetActionCooldown() > 55)
                        {
                            return new CheckResult(true, "团辅外溢出 可以打一个");
                        }
                        
                        return new CheckResult(false, "留锤子进团辅");
                    }

                    if (PCTSkill.武器构想.GetActionCharges() >= 1)
                    {
                        return new CheckResult(true, "正常打一个");
                    }

                    if (!PromeSettings.Instance.GetQt(PCTQt.星空构想) && PCTSkill.武器构想.GetActionCharges() >= 2)
                    {
                        return new CheckResult(true, "即将溢出");
                    }
                }
                
                // 一层的逻辑
                if (player.Level < 86)
                {
                    if (PCTSkill.武器构想.GetActionCooldown() == 0)
                    {
                        return new CheckResult(true, "转好了 直接打掉");
                    }
                }
                
                // 这里是这样判断的？可以直接用程序从上到下执行的特性来写一个三秒的锤子开启？
                /*if (PCTSkill.武器构想.GetActionCooldown() < PCTSkill.风景构想.GetActionCooldown() - 6)
                {
                    return new CheckResult(false, "团辅前可以打一个 防止溢出");
                }*/
                
                return new CheckResult(false, "准备打进团辅");
            }
            
            return new CheckResult(true, "有一个打掉");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.武器构想, ActionType.OffGcd, ActionTargetType.Self);
    }
}
