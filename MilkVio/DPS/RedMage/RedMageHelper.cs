using Dalamud.Game.ClientState.Objects.Types;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.DPS.RedMage.RDMData;

namespace MilkVio.DPS.RedMage;

public static class RedMageHelper
{
    public static ManaType GetLowestManaType()
    {
        var blackMana = JobGaugeHelper.RDM.BlackMana;
        var whiteMana = JobGaugeHelper.RDM.WhiteMana;
        if (blackMana > whiteMana) return ManaType.WhiteMana;
        return ManaType.BlackMana;
    }

    public static bool HasRedFireReady(IBattleChara me)
    {
        return me.HasStatus(RDMBuff.赤火炎预备);
    }
    
    public static bool HasRedStoneReady(IBattleChara me)
    {
        return me.HasStatus(RDMBuff.赤飞石预备);
    }
    
    public static bool HasContinuousCast(IBattleChara me)
    {
        return me.HasStatus(RDMBuff.连续咏唱);
    }
    
    public static uint GetCurrentSingleThunderActionId(IBattleChara me)
    {
        if (me.Level < 82) return RDMSkill.赤闪雷;
        return RDMSkill.赤暴雷;
    }
    
    public static uint GetCurrentShakeActionId(IBattleChara me)
    {
        if (me.Level < 84) return RDMSkill.震荡;
        return RDMSkill.激荡;
    }
    
    public static uint GetCurrentSingleWindActionId(IBattleChara me)
    {
        if (me.Level < 82) return RDMSkill.赤疾风;
        return RDMSkill.赤暴风;
    }

    public static uint GetNextManaActionPhase(bool isAoe)
    {
        var lastComboId = ActionHelper.GetLastComboID();
        
        if (isAoe)
        {
            switch (lastComboId)
            {
                case RDMSkill.魔划圆斩1:
                    return 2;
                case RDMSkill.魔划圆斩2:
                    return 3;
                default: return 1;
            }
        }

        switch (lastComboId)
        {
            case RDMSkill.魔回刺1:
                return 2;
            case RDMSkill.魔交击斩2:
                return 3;
            default: return 1;
        }
    }

    public static uint GetManaActionStack()
    {
        return JobGaugeHelper.RDM.ManaStacks;
    }

    public static bool IsInManaActionCombo(IBattleChara me)
    {
        var lastComboId = ActionHelper.GetLastComboID();
        var level = me.Level;
        
        if(GetManaActionStack() > 0) return true;
        
        if (level < 80)
        {
            return false;
        }

        if (level >= 80 && level < 90)
        {
            if (lastComboId == RDMSkill.赤神圣 || lastComboId == RDMSkill.赤核爆) return true;
        }
        
        if (level >= 90)
        {
            if(lastComboId == RDMSkill.赤神圣 || lastComboId == RDMSkill.赤核爆 || lastComboId == RDMSkill.焦热) return true;
        }
        
        return false;
    }

    public static bool CanUseMana3Action(IBattleChara me)
    {
        if (JobGaugeHelper.RDM.BlackMana >= 50 && JobGaugeHelper.RDM.WhiteMana >= 50) return true;
        if (me.HasStatus(RDMBuff.魔法剑术)) return true;
        return false;
    }
    
    public static float GetPromoteStack()
    {
        var player = Core.Me;
        float MaxCharges = 2f;  // 最大层数
        float PerCharge  = 55f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = RDMSkill.促进.GetActionCooldown();
        
        // 等级适配
        if (player.Level < 88)
        {
            MaxCharges = 1f;
            //cdToFull -= 1 * PerCharge;
        }
        
        // 防止出界
        if (cdToFull < 0f) cdToFull = 0f;
        if (cdToFull > MaxCharges * PerCharge) cdToFull = MaxCharges * PerCharge;

        // 计算
        float real = MaxCharges - (cdToFull / PerCharge);
        if (real < 0f) real = 0f;
        if (real > MaxCharges) real = MaxCharges;

        return real;
    }
}
