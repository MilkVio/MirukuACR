using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;
using MilkVio.DPS.RedMage.RDMData;


namespace MilkVio.DPS.RedMage.Action.OffGcd;

public class 促进OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(RDMQt.促进)) return new CheckResult(false, "未开启促进");
        
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        if (RedMageHelper.HasContinuousCast(me)) return new CheckResult(false, "当前有连续咏唱");
        if (RedMageHelper.IsInManaActionCombo(me)) return new CheckResult(false, "正在魔三连/焦热决断");
        if (me.HasStatus(RDMBuff.即刻咏唱)) return new CheckResult(false, "当前有即刻咏唱");
        if (me.HasStatus(RDMBuff.促进)) return new CheckResult(false, "当前有促进");
        var cd120 = RDMSkill.鼓励.GetActionCooldown();
        if (cd120 < 10 && !PromeSettings.Instance.GetQt(RDMQt.不打魔连击)) 
        {
            return new CheckResult(false, "魔三连要好了 不打");
        }

        if (me.HasStatus(RDMBuff.魔法剑术)) return new CheckResult(false, "身上有魔法剑术 不打");
        
        if (me.HasStatus(RDMBuff.显贵冲击预备)) return new CheckResult(false, "身上有显贵冲击 不打");
        
        var stack = RedMageHelper.GetPromoteStack();
        if (stack >= 1)
        {
            if (PromeSettings.Instance.GetQt(RDMQt.倾泻资源))
            {
                return new CheckResult(true, "倾泻资源 有就打");
            }
            else
            {
                /*
                 * 第2种情况 层数满了 打掉
                 * 第1种情况 火炎飞石都没有被触发 打掉
                 */
                if (!RedMageHelper.HasRedStoneReady(me) && !RedMageHelper.HasRedFireReady(me))
                    return new CheckResult(true, "无触发 打掉");
                if (me.Level < 88 && stack >= 1) return new CheckResult(true, "80级满了就打");
                if (me.Level >= 88 && stack >= 2) return new CheckResult(true, "100级满了就打");
            }
        }
        
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(RDMSkill.促进, ActionType.OffGcd, ActionTargetType.Target);
    }
}
