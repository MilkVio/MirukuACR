using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.OffGcd;

public class 动物构想OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (!JobGaugeHelper.PCT.CreatureMotifDrawn) return new CheckResult(false, "未拥有生物画");
        if (PctHelper.GetCurrentCreatureCharge() < 1) return new CheckResult(false, "未冷却");
        
        var player = Core.Me;
        var coolDown120 = PCTSkill.风景构想.GetActionCooldown();
        
        // 自身不可以执行的条件
        if (IsMutualExclusion()) return new CheckResult(false, "与当前动物炮互斥");
        if (PCTSkill.动物构想.GetActionCharges() < 1) return new CheckResult(false, "未冷却");
        
        // Qt控制
        if (!PromeSettings.Instance.GetQt(PCTQt.动物构想)) return new CheckResult(false, "未开启QT");
        
        if (PromeSettings.Instance.GetQt(PCTQt.倾泻资源))
        {
            return new CheckResult(true, "已开启倾泻资源");
        }
        
        if (player.HasStatus(PCTBuff.星空构想))
        {
            // 这里为了对其其他人的 最大化收益
            if (player.Level >= 96)
            {
                if (PctHelper.GetCurrentCreatureCharge() == 3)
                {
                    return new CheckResult(true, "团副内 溢出");
                }
            }
            else
            {
                if (PctHelper.GetCurrentCreatureCharge() == 2)
                {
                    return new CheckResult(true, "团副内 溢出");  
                }
            }
            
            if (StatusHelper.GetStatusLeftTime(player, PCTBuff.星空构想) < 15)
            {
                return new CheckResult(true, "团副内");
            }
        }
        
        if (player.Level >= 96)
        {
            if (coolDown120 <= 40)
            {
                // 如果在20-40秒之间 同时这个>1.8层 代表即将溢出 打一个
                if (coolDown120 > 20 && PctHelper.GetCurrentCreatureCharge() > 1.8)
                {
                    return new CheckResult(true, "打");
                }
                return new CheckResult(false, "留一个进团辅");
            }

            if (PctHelper.GetCurrentCreatureCharge() > 2)
            {
                return new CheckResult(true, "打");
            }
        }

        if (player.Level < 96)
        {
            if (coolDown120 <= 40)
            {
                // 如果在20-40秒之间 同时这个>1.8层 代表即将溢出 打一个
                if (coolDown120 > 20 && PctHelper.GetCurrentCreatureCharge() > 1.5)
                {
                    return new CheckResult(true, "打");
                }
                
                return new CheckResult(false, "留一个进团辅");
            }
            
            if (PctHelper.GetCurrentCreatureCharge() > 1)
            {
                return new CheckResult(true, "打");
            }
        }
        
        return new CheckResult(false, "冷却但未满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PCTSkill.动物构想, ActionType.OffGcd, ActionTargetType.Target);
    }

    private bool IsMutualExclusion()
    {
        // 莫古炮好的时候 不能用尖牙
        if (JobGaugeHelper.PCT.MooglePortraitReady)
        {
            if(PctHelper.GetCurrentCreature() == Creatures.Claw) return true;
            if(PctHelper.GetCurrentCreature() == Creatures.Wings) return true;
        }
        
        // 马蒂恩惩罚好的时候 不能用翅膀
        if (JobGaugeHelper.PCT.MadeenPortraitReady)
        {
            if(PctHelper.GetCurrentCreature() == Creatures.Pom) return true;
        }
        // ↑ 这里统一用前一个画？
        return false;
    }
}
