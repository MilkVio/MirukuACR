using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Action.OffGcd;

public class 生物炮OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var canUse = PctHelper.IsMoogleCannonReady() || PctHelper.IsMadeenSmReady();
        
        if (!canUse) return new CheckResult(false, "未拥有动物炮");
        
        var player = Core.Me;
        var coolDown120 = PCTSkill.风景构想.GetActionCooldown();
        
        // Qt控制
        if (!PromeSettings.Instance.GetQt(PCTQt.动物炮)) return new CheckResult(false, "未开启QT");
        if (JobGaugeHelper.PCT.MooglePortraitReady && PCTSkill.莫古力激流.GetActionCooldown() != 0) return new CheckResult(false, "莫古力激流未冷却");
        if (JobGaugeHelper.PCT.MadeenPortraitReady && PCTSkill.马蒂恩惩罚.GetActionCooldown() != 0) return new CheckResult(false, "马蒂恩惩罚未冷却");
        
        // todo
        /* 大体逻辑↓ 初版
         * 基本上大概就是优先级+互斥的关系 生物炮的优先级高 如果检测到生物构想满了+还没有到到达团辅，那么生物炮用 互斥解除 生物构想也能继续使用
         * 按等级区分层数？
         * 主要是要保证在团辅里面至少打一个生物构想（全部为800威力）
         * 
         */
        
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
        
        // 这里不能简单的判断团辅时间 要根据目前的状态决定。 todo
        // 如果动物炮转好了，代表下一个动物炮一定需要两个动物彩绘，也就意味着40*2才有一个动物炮
        // 所以这里只需要计算 实际？不对，这里可能需要根据等级计算总CD？
        // 这里似乎可以粗略计算为 A值为：（实际动物构想次数 + 1（为团辅内的一个动物构想）） * 40 为冷却时间
        // 
        /*if (coolDown120 > 60)
        {
            return new CheckResult(true, "团辅>60 打");
        }*/

        if (player.Level >= 96)
        {
            if (coolDown120 <= 40 && coolDown120 > 10)
            {
                if (PctHelper.GetCurrentCreatureCharge() > 2)
                {
                    return new CheckResult(true, "团辅期之前要溢出了 打掉");
                }
            }

            if (PctHelper.GetCurrentCreatureCharge() > 2.95)
            {
                return new CheckResult(true, "团辅外到达两个 打掉");
            }
        }
        
        //90级
        if (player.Level < 96)
        {
            if (coolDown120 <= 40 && coolDown120 > 10)
            {
                if (PctHelper.GetCurrentCreatureCharge() > 1.75)
                {
                    return new CheckResult(true, "团辅期之前要溢出了 打掉");
                }
            }

            if (PctHelper.GetCurrentCreatureCharge() > 1.9)
            {
                return new CheckResult(true, "团辅外到达两个 打掉");
            }
        }
        
        // 如果这里即将溢出了
        if (IsOverFlow())
        {
            if (coolDown120 < 10)
            {
                return new CheckResult(false, "溢出也等进团辅");
            }
            return new CheckResult(true, "要溢出了 直接打掉");
        }
        
        return new CheckResult(false, "不满足任何条件");
    }

    public PAction GetAction()
    {
        if (PctHelper.IsMadeenSmReady()) return new PAction(PCTSkill.马蒂恩惩罚, ActionType.OffGcd, ActionTargetType.Target);
        return new PAction(PCTSkill.莫古力激流, ActionType.OffGcd, ActionTargetType.Target);
    }

    private bool IsOverFlow()
    {
        var player = Core.Me;
        
        // 96级三层
        if (player.Level >= 96)
        {
            if (PctHelper.GetCurrentCreatureCharge() > 2.95)
            {
                return true;
            }
        }
        // 以下两层
        if (PctHelper.GetCurrentCreatureCharge() > 1.95) return true;
        
        return false;
    }

    private int GetLeastCreatureStack4Canno()
    {
        if (PctHelper.IsMoogleCannonReady())
        {
            if (PctHelper.GetCurrentCreature() == Creatures.Claw)
            {
                return 1;
            }

            return 2;
        }

        if (PctHelper.IsMadeenSmReady())
        {
            if (PctHelper.GetCurrentCreature() == Creatures.Pom)
            {
                return 1;
            }

            return 2;
        }

        return 2;
    }
}
