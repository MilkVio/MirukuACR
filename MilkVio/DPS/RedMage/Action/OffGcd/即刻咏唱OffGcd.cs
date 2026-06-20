using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;
using MilkVio.DPS.RedMage.RDMData;


namespace MilkVio.DPS.RedMage.Action.OffGcd;

public class 即刻咏唱OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(RDMQt.即刻咏唱)) return new CheckResult(false, "未开启即刻咏唱");
        
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        if (RedMageHelper.HasContinuousCast(me)) return new CheckResult(false, "当前有连续咏唱");
        if (RedMageHelper.IsInManaActionCombo(me)) return new CheckResult(false, "正在魔三连/焦热决断");
        if (me.HasStatus(RDMBuff.促进)) return new CheckResult(false, "当前有促进");
        if (JobGaugeHelper.RDM.WhiteMana >= 50 && JobGaugeHelper.RDM.BlackMana >= 50 &&
            !PromeSettings.Instance.GetQt(RDMQt.不打魔连击))
        {
            return new CheckResult(false, "魔三连要好了 不打");
        }

        if (me.HasStatus(RDMBuff.魔法剑术)) return new CheckResult(false, "身上有魔法剑术 不打");
        
        var cd = RDMSkill.即刻咏唱.GetActionCooldown();
        var cd120 = RDMSkill.鼓励.GetActionCooldown();
        var isCan120Use = cd120 < 120 && cd120 > 45;
        if (cd == 0)
        {
            if (PromeSettings.Instance.GetQt(RDMQt.倾泻资源))
            {
                return new CheckResult(true, "冷却了打掉");
            }
            if (isCan120Use) return new CheckResult(true, "冷却了打掉");
            
            if (StatusHelper.GetStatusLeftTime(me, RDMBuff.鼓励) < 4) return new CheckResult(true, "团辅最后一G打一个");
        }
        
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(RDMSkill.即刻咏唱, ActionType.OffGcd, ActionTargetType.Target);
    }
}
