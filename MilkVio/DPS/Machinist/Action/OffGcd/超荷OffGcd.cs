using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Action.OffGcd;

public class 超荷OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var me = Core.Me;
        var isCanUse = !me.HasStatus(MCHStatus.过热) && !me.HasStatus(MCHStatus.整备) &&
                       MCHSkill.超荷.GetActionCooldown() == 0 && MachinistHelper.Get超荷Count() >= 1;

        if (!PromeSettings.Instance.GetQt(MCHQt.超荷)) return new CheckResult(false, "未开启QT");
        
        if (isCanUse)
        {
            var 空气锚cd = MCHSkill.空气锚.GetActionCooldown();
            var 钻头cd = MCHSkill.钻头.GetActionCooldown();
            var 回转飞锯cd = MCHSkill.回转飞锯.GetActionCooldown();
            var 野火cd = MCHSkill.野火.GetActionCooldown();
            var is野火状态 = MCHSkill.野火.GetActionCooldown() >= 110;

            if (PromeSettings.Instance.GetQt(MCHQt.倾泻资源))
            {
                if (空气锚cd < 8 || 钻头cd < 8 || (me.Level >= 90 && 回转飞锯cd < 8))
                {
                    return new CheckResult(false, "有大技能CD");
                }
                return new CheckResult(true, "倾泻资源");
            }
            
            if (野火cd < 30 && MachinistHelper.Get超荷Count() < 2 && PromeSettings.Instance.GetQt(MCHQt.野火))
            {
                return new CheckResult(false, "攒资源");
            }

            if (野火cd < 8 && PromeSettings.Instance.GetQt(MCHQt.野火))
            {
                return new CheckResult(false, "等团辅");
            }
            
            if (is野火状态)
            {
                return new CheckResult(true, "野火状态直接打 不管大技能");
            }
            
            if (野火cd > 100 && (me.HasStatus(MCHStatus.全金属爆发预备) || me.HasStatus(MCHStatus.掘地飞轮预备)))
            {
                return new CheckResult(false, "等大技能");
            }
            
            if (空气锚cd < 8 || 钻头cd < 8 || (me.Level >= 90 && 回转飞锯cd < 8))
            {
                return new CheckResult(false, "有大技能CD");
            }
            
            return new CheckResult(true, "打一个");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(MCHSkill.超荷, ActionType.OffGcd, ActionTargetType.Self);
    }
}
