using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Action.OffGcd;

public class 机器人OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var me = Core.Me;
        if(me.DistanceToMe() > currentAttackRange) return new CheckResult(false, "距离过远");
        var battery = JobGaugeHelper.MCH.Battery;
        var isCanUse = battery >= 50 && !JobGaugeHelper.MCH.IsRobotActive;

        if (!PromeSettings.Instance.GetQt(MCHQt.机器人)) return new CheckResult(false, "未开启QT");
        
        if (MCHSkill.野火.GetActionCooldown() > 115 && !me.HasStatus(MCHStatus.过热))
            return new CheckResult(false, "团辅防抢过热");

        if (MachinistHelper.GetCurrentRobotActionId().GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        
        if (isCanUse)
        {
            var 空气锚cd = MCHSkill.空气锚.GetActionCooldown();
            var 钻头cd = MCHSkill.钻头.GetActionCooldown();
            var 回转飞锯cd = MCHSkill.回转飞锯.GetActionCooldown();
            var 野火cd = MCHSkill.野火.GetActionCooldown();
            var is野火状态 = MCHSkill.野火.GetActionCooldown() >= 110;

            if (PromeSettings.Instance.GetQt(MCHQt.倾泻资源))
            {
                return new CheckResult(true, "倾泻资源");
            }
            // 这里做一个防溢出
            if (battery == 90)
            {
                if (空气锚cd < 10 || 回转飞锯cd < 10 || me.HasStatus(MCHStatus.掘地飞轮预备))
                {
                    return new CheckResult(true, "即将溢出 直接打");
                }
            }
            
            // 小于爆发小于30秒攒资源
            if (野火cd < 30 && battery != 100 && PromeSettings.Instance.GetQt(MCHQt.野火))
            {
                return new CheckResult(false, "攒资源");
            }
            
            
            if (野火cd < 8 && PromeSettings.Instance.GetQt(MCHQt.野火) && 野火cd != 0 && battery <= 80)
            {
                if (空气锚cd < 4 || 回转飞锯cd < 4)
                {
                    return new CheckResult(false, "等两个大伤害");
                }
                
            }
            
            if (野火cd < 4 && PromeSettings.Instance.GetQt(MCHQt.野火))
            {
                return new CheckResult(true, "野火状态直接打 不管大技能");
            }
            
            if (is野火状态)
            {
                return new CheckResult(true, "野火状态直接打 不管大技能");
            }
            
            return new CheckResult(true, "打一个");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(MachinistHelper.GetCurrentRobotActionId(), ActionType.OffGcd, ActionTargetType.Self);
    }
}
