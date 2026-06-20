using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist;

public static class MachinistHelper
{
    public static PAction GetBaseAction()
    {
        var lastComboId = ActionHelper.GetLastComboID();
        switch (lastComboId)
        {
            case MCHSkill.分裂弹1:
                return new PAction(MCHSkill.热独头弹2, ActionType.Gcd, ActionTargetType.Target);
            case MCHSkill.独头弹2:
                return new PAction(MCHSkill.热狙击弹3, ActionType.Gcd, ActionTargetType.Target);
            default:
                return new PAction(MCHSkill.热分裂弹1, ActionType.Gcd, ActionTargetType.Target);
        }
    }

    public static uint Get空气锚CurrentId()
    {
        var level = Core.Me.Level;
        if (level < 76) return MCHSkill.热弹;
        return MCHSkill.空气锚;
    }
    
    public static uint Get虹吸弹CurrentId()
    {
        var level = Core.Me.Level;
        if (level < 92) return MCHSkill.虹吸弹;
        return MCHSkill.双将;
    }
    
    public static uint Get弹射CurrentId()
    {
        var level = Core.Me.Level;
        if (level < 92) return MCHSkill.弹射;
        return MCHSkill.将死;
    }
    
    public static float GetCurrent整备Charge()
    {
        var player = Core.Me;
        float MaxCharges = 2f;  // 最大层数
        float PerCharge  = 55f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = MCHSkill.整备.GetActionCooldown();
        
        // 等级适配
        if (player.Level < 84)
        {
            MaxCharges = 1f;
            //cdToFull -= 1 * PerCharge;
        }
        
        // 防止出界（如果小于0或大于80）
        if (cdToFull < 0f) cdToFull = 0f;
        if (cdToFull > MaxCharges * PerCharge) cdToFull = MaxCharges * PerCharge;

        // 计算浮
        float real = MaxCharges - (cdToFull / PerCharge);
        if (real < 0f) real = 0f;
        if (real > MaxCharges) real = MaxCharges;

        return real;
    }
    
    public static float GetCurrent钻头Charge()
    {
        var player = Core.Me;
        float MaxCharges = 2f;  // 最大层数
        float PerCharge  = 20f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = MCHSkill.钻头.GetActionCooldown();
        
        // 等级适配
        if (player.Level < 94)
        {
            MaxCharges = 1f;
            //cdToFull -= 1 * PerCharge;
        }
        
        // 防止出界（如果小于0或大于80）
        if (cdToFull < 0f) cdToFull = 0f;
        if (cdToFull > MaxCharges * PerCharge) cdToFull = MaxCharges * PerCharge;

        // 计算浮
        float real = MaxCharges - (cdToFull / PerCharge);
        if (real < 0f) real = 0f;
        if (real > MaxCharges) real = MaxCharges;

        return real;
    }

    public static uint Get超荷Count()
    {
        var player = Core.Me;
        var heat = JobGaugeHelper.MCH.Heat;
        var count = 0;
        if (player.HasStatus(MCHStatus.超荷预备)) count += 1;
        if (heat >= 50 && heat != 100)
        {
            count += 1;
        }

        if (heat == 100)
        {
            count += 2;
        }
        
        return (uint)count;
    }

    public static uint GetCurrentRobotActionId()
    {
        var level = Core.Me.Level;
        if (level < 80)
        {
            return MCHSkill.车式浮空炮塔;
        }

        return MCHSkill.后式自走人偶;
    }
    
    public static uint GetCurrentRobotBurstActionId()
    {
        var level = Core.Me.Level;
        if (level < 80)
        {
            return MCHSkill.超档车式炮塔;
        }

        return MCHSkill.超档车式炮塔;
    }
}
