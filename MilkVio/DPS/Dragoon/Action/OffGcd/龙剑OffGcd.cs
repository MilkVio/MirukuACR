using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon.Action.OffGcd;

public class 龙剑OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        // 前置条件
        if(Core.Me.HasStatus(DRGBuff.龙剑)) return new CheckResult(false, "自身已存在龙剑");
        
        if (ActionHelper.GetLastComboID() == DragoonHelper.GetCurrentStraightComboActionId(2) ||
            ActionHelper.GetLastComboID() == DragoonHelper.GetCurrentStraightComboActionId(4) ||
            ActionHelper.GetLastComboID() == DragoonHelper.GetCurrentSakuraComboActionId(4))
            
        {
            // 龙剑没有两层
            if (Core.Me.Level < 88)
            {
                // what the fuck
                // 为什么这里龙剑会他妈的为复数
                if (DRGSkill.龙剑.GetActionCooldown() == 0)
                {
                    return new CheckResult(true, "下一个为高威力技能 90");
                }
            }
            else if (Core.Me.Level > 88)
            {
                if (DRGSkill.龙剑.GetActionCharges() >= 1)
                {
                    return new CheckResult(true, "下一个为高威力技能 100");
                }
            }
            return new CheckResult(false, "下一个Combo为高威力技能 但是没有龙剑");
        }
        
        return new CheckResult(false, "龙剑数量不足");
    }

    public PAction GetAction()
    {
        return new PAction(DRGSkill.龙剑, ActionType.OffGcd, ActionTargetType.Self);
    }
}
