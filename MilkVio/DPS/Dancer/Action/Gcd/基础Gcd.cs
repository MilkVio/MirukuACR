using ECommons.DalamudServices;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;

namespace MilkVio.DPS.Dancer.Action.Gcd;

public class 基础Gcd : IDecisionResolver //12 强化12 大舞小舞结束动作落幕舞
{
    // 检查什么时候可以用这个技能
    // 如果检查通过 就执行下面的getaction
    public CheckResult Check()
    {
        var gcd和目标距离 = GameData.GetCurrentAttackRange(25f);

        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        if (Core.Me.DistanceToMe() <= gcd和目标距离)
        {
            return new CheckResult(true, "距离 <= 25");
        }

        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var me = Core.Me;
        
        //小舞续剑
        if (me.HasStatus(DNCBuff.落幕舞预备))
        {
            return new PAction(DNCSkill.落幕舞, ActionType.Gcd, ActionTargetType.Target);
        }
        
         //强化1
        if (me.HasStatus(DNCBuff.对称投掷) || me.HasStatus(DNCBuff.百花对称投掷) == true)
        {
            return new PAction(DNCSkill.逆瀑泻, ActionType.Gcd, ActionTargetType.Target);
        }
         //强化2
        if (me.HasStatus(DNCBuff.非对称投掷) || me.HasStatus(DNCBuff.百花非对称投掷) == true)
        {
            return new PAction(DNCSkill.坠喷泉, ActionType.Gcd, ActionTargetType.Target);
        }
         //2
        if (ActionHelper.GetLastComboID() == DNCSkill.瀑泻)
        {
            return new PAction(DNCSkill.喷泉, ActionType.Gcd, ActionTargetType.Target);
        }

        return new PAction(DNCSkill.瀑泻, ActionType.Gcd, ActionTargetType.Target);
    }
}
