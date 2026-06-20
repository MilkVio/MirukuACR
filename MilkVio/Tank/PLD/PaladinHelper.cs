using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD;

public static class PaladinHelper
{
    public static PAction GetBaseGcd()
    {
        var lastActionId = ActionHelper.GetLastComboID();
        switch (lastActionId)
        {
            case PLDSkill.先锋剑:
                return new PAction(PLDSkill.暴乱剑, ActionType.Gcd, ActionTargetType.Target);
            case PLDSkill.暴乱剑:
                return new PAction(PLDSkill.王权剑, ActionType.Gcd, ActionTargetType.Target);
        }
        return new PAction(PLDSkill.先锋剑, ActionType.Gcd, ActionTargetType.Target);
    }

    public static PAction? GetBoostBaseGcd()
    {
        var me = Core.Me;
        if (me.HasStatus(PLDBuff.赎罪剑预备))
        {
            return new PAction(PLDSkill.赎罪剑, ActionType.Gcd, ActionTargetType.Target);
        }
        if (me.HasStatus(PLDBuff.祈告剑预备))
        {
            return new PAction(PLDSkill.祈告剑, ActionType.Gcd, ActionTargetType.Target);
        }
        if (me.HasStatus(PLDBuff.葬送剑预备))
        {
            return new PAction(PLDSkill.葬送剑, ActionType.Gcd, ActionTargetType.Target);
        }

        return null;
    }

    public static bool HasBlade()
    {
        var me = Core.Me;
        if (me.HasStatus(PLDBuff.悔罪预备))
        {
            return true;
        }

        if (ActionHelper.GetAdjustedActionId(PLDSkill.悔罪) != PLDSkill.悔罪)
        {
            return true;
        }
        
        return false;
    }

    public static PAction GetAdjusteBlade()
    {
        switch (ActionHelper.GetAdjustedActionId(PLDSkill.悔罪))
        {
            case PLDSkill.信念之剑:
                return new PAction(PLDSkill.信念之剑, ActionType.Gcd, ActionTargetType.Target);
            case PLDSkill.真理之剑:
                return new PAction(PLDSkill.真理之剑, ActionType.Gcd, ActionTargetType.Target);
            case PLDSkill.英勇之剑:
                return new PAction(PLDSkill.英勇之剑, ActionType.Gcd, ActionTargetType.Target);
        }
        return new PAction(PLDSkill.悔罪, ActionType.Gcd, ActionTargetType.Target);
    }

    public static bool IsBladeInLimitTime()
    {
        var time = Core.Me.GetStatusLeftTime(PLDBuff.战逃反应buff);
        if(time == 0) return false;
        if(time < 11f &&　time > 0) return true;
        return false;
    }
    public static uint GetAdjustedAnhunId()
    {
        var me = Core.Me;
        if (me.Level < 96)
        {
            return PLDSkill.安魂祈祷技能;
        }
        return PLDSkill.绝对统治;
    }

    public static uint GetHighDamageCount()
    {
        var me = Core.Me;
        uint count = 0;
        if(me.HasStatus(PLDBuff.赎罪剑预备)) count = 3;
        if(me.HasStatus(PLDBuff.祈告剑预备)) count = 2;
        if(me.HasStatus(PLDBuff.葬送剑预备)) count = 1;
        if (me.HasStatus(PLDBuff.神圣魔法效果提高)) count += 1;
        if (ActionHelper.GetLastComboID() == PLDSkill.暴乱剑) count += 1;
        if (me.HasStatus(PLDBuff.神圣魔法效果提高) && ActionHelper.GetLastComboID() == PLDSkill.暴乱剑) count = 3;
        if (me.HasStatus(PLDBuff.葬送剑预备) && ActionHelper.GetLastComboID() == PLDSkill.暴乱剑) count = 3;
        return count;
    }
    
    // 如果开启最优战逃
    // 有几种情况 总之要在战逃内保证可以打出 3个高威力
    // 所以为了应对各种情况 在此我们需要列举出来符合条件的情况
    /*
     * 解决了 总之先用上面那个函数看看
     * 因为这里只是看开启时机 具体的求解技能还要在其他求解器设置
     */
    public static bool IsCanUse60()
    {
        var me = Core.Me;
        var count = GetHighDamageCount();
        // 等级限制
        if (me.Level >= 76)
        {
            if(count >= 3) return true;
        }
        else
        {
            if (ActionHelper.GetLastComboID() == PLDSkill.王权剑)
            {
                return true;
            }
        }
        
        return false;
    }
    
    public static PAction? AutoIron(bool mode)
    {
        var player = Core.Me;
        if (player == null) return null;
        
        if (mode)
        {
            if (!player.HasStatus(PLDBuff.钢铁信念buff))
            {
                return new PAction(PLDSkill.钢铁信念, ActionType.OffGcd, ActionTargetType.Self);
            }

            return null;
        }

        if (mode == false)
        {
            if (player.HasStatus(PLDBuff.钢铁信念buff))
            {
                return new PAction(PLDSkill.解除钢铁信念, ActionType.OffGcd, ActionTargetType.Self);;
            }
        }

        return null;
    }
}
