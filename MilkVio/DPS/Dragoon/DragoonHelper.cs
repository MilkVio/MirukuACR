using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon;

public static class DragoonHelper
{
    public static PAction? GetCurrentStraightComboPAction(int combo)
    {
        var player = Core.Me;
        if (player == null) return null;
        
        switch (combo)
        {
            case 1:
                return new PAction(DRGSkill.精准刺, ActionType.Gcd, ActionTargetType.Target);
            
            case 2:
                if (player.Level >= 96) return new PAction(DRGSkill.前冲刺, ActionType.Gcd, ActionTargetType.Target);
                return new PAction(DRGSkill.贯通刺, ActionType.Gcd, ActionTargetType.Target);
            
            case 3:
                if (player.Level >= 86) return new PAction(DRGSkill.苍穹刺, ActionType.Gcd, ActionTargetType.Target);
                return new PAction(DRGSkill.直刺, ActionType.Gcd, ActionTargetType.Target);
            
            case 4:
                return new PAction(DRGSkill.龙牙龙爪, ActionType.Gcd, ActionTargetType.Target);
            
            default:
                return null;
        }
    }
    
    public static uint? GetCurrentStraightComboActionId(int combo)
    {
        var player = Core.Me;
        if (player == null) return null;
        
        switch (combo)
        {
            case 1:
                return DRGSkill.精准刺;
            
            case 2:
                if (player.Level >= 96) return DRGSkill.前冲刺;
                return DRGSkill.贯通刺;
            
            case 3:
                if (player.Level >= 86) return DRGSkill.苍穹刺;
                return DRGSkill.直刺;
            
            case 4:
                return DRGSkill.龙牙龙爪;
            
            default:
                return null;
        }
    }
    
    public static PAction? GetCurrentSakuraComboPAction(int combo)
    {
        var player = Core.Me;
        if (player == null) return null;
        
        switch (combo)
        {
            case 1:
                return new PAction(DRGSkill.精准刺, ActionType.Gcd, ActionTargetType.Target);
            
            case 2:
                if (player.Level >= 96) return new PAction(DRGSkill.螺旋击, ActionType.Gcd, ActionTargetType.Target);
                return new PAction(DRGSkill.开膛枪, ActionType.Gcd, ActionTargetType.Target);
            
            case 3:
                if (player.Level >= 86) return new PAction(DRGSkill.樱花缭乱, ActionType.Gcd, ActionTargetType.Target);
                return new PAction(DRGSkill.樱花怒放, ActionType.Gcd, ActionTargetType.Target);
            
            case 4:
                return new PAction(DRGSkill.龙尾大回旋, ActionType.Gcd, ActionTargetType.Target);
            
            default:
                return null;
        }
    }
    
    public static uint? GetCurrentSakuraComboActionId(int combo)
    {
        var player = Core.Me;
        if (player == null) return null;
        
        switch (combo)
        {
            case 1:
                return DRGSkill.精准刺;
            
            case 2:
                if (player.Level >= 96) return DRGSkill.螺旋击;
                return DRGSkill.开膛枪;
            
            case 3:
                if (player.Level >= 86) return DRGSkill.樱花缭乱;
                return DRGSkill.樱花怒放;
            
            case 4:
                return DRGSkill.龙尾大回旋;
            
            default:
                return null;
        }
    }
    
    /// <summary>
    /// 获取下一个连击的技能
    /// </summary>
    /// <returns>PAction</returns>
    public static PAction? GetNextComboPAction()
    {
        var player = Core.Me;
        var comboActionId = ActionHelper.GetLastComboID();
        if(player == null) return null;
        
        if(comboActionId == GetCurrentStraightComboActionId(1)) return GetCurrentStraightComboPAction(2);
        if(comboActionId == GetCurrentStraightComboActionId(2)) return GetCurrentStraightComboPAction(3);
        if(comboActionId == GetCurrentStraightComboActionId(3)) return GetCurrentStraightComboPAction(4);
        if(comboActionId == GetCurrentSakuraComboActionId(1)) return GetCurrentSakuraComboPAction(2);
        if(comboActionId == GetCurrentSakuraComboActionId(2)) return GetCurrentSakuraComboPAction(3);
        if(comboActionId == GetCurrentSakuraComboActionId(3)) return GetCurrentSakuraComboPAction(4);
        if(comboActionId == GetCurrentStraightComboActionId(4)) return new PAction(DRGSkill.云蒸龙变, ActionType.Gcd, ActionTargetType.Target);
        if(comboActionId == GetCurrentSakuraComboActionId(4)) return new PAction(DRGSkill.云蒸龙变, ActionType.Gcd, ActionTargetType.Target);
        if (comboActionId == DRGSkill.云蒸龙变 && player.HasStatus(DRGBuff.龙眼)) return new PAction(DRGSkill.龙眼雷电, ActionType.Gcd, ActionTargetType.Target);
        return new PAction(DRGSkill.精准刺, ActionType.Gcd, ActionTargetType.Target);
    }

    public static PAction GetNextAOEComboPAction()
    {
        var player = Core.Me;
        var comboActionId = ActionHelper.GetLastComboID();
        if(comboActionId == DRGSkill.死天枪) return new PAction(DRGSkill.音速刺, ActionType.Gcd, ActionTargetType.Target);
        if(comboActionId == DRGSkill.龙眼雷电) return new PAction(DRGSkill.音速刺, ActionType.Gcd, ActionTargetType.Target);
        
        if (player.Level >= 72)
        {
            if(comboActionId == DRGSkill.音速刺) return new PAction(DRGSkill.山境酷刑, ActionType.Gcd, ActionTargetType.Target);
        }
        if (player.Level >= 82 && player.HasStatus(DRGBuff.龙眼))
        {
            return new PAction(DRGSkill.龙眼苍穹, ActionType.Gcd, ActionTargetType.Target);
        }
        
        return new PAction(DRGSkill.死天枪, ActionType.Gcd, ActionTargetType.Target);
    }
}
