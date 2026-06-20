using Dalamud.Game.ClientState.JobGauge.Enums;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer;

public static class PctHelper
{
    /// <summary>
    /// 获取基础GCD
    /// </summary>
    /// <param name="isReversed">是否在反转状态</param>
    /// <param name="isAoe">是否为AOE</param>
    /// <returns></returns>
    public static PAction GetBaseGcd(bool isReversed, bool isAoe)
    {
        var player = Core.Me;
        // AOE模式
        if (isAoe)
        {
            // 不在反转状态
            if (!isReversed)
            {
                if(player.HasStatus(PCTBuff.以太色调)) return new PAction(PCTSkill.烈风之绿2, ActionType.Gcd, ActionTargetType.Target);
                if(player.HasStatus(PCTBuff.以太色调2)) return new PAction(PCTSkill.激水之蓝3, ActionType.Gcd, ActionTargetType.Target);
                return new PAction(PCTSkill.烈炎之红1, ActionType.Gcd, ActionTargetType.Target);
            }
        
            // 在反转状态
            if(player.HasStatus(PCTBuff.以太色调)) return new PAction(PCTSkill.坚石之纯黄2, ActionType.Gcd, ActionTargetType.Target);
            if(player.HasStatus(PCTBuff.以太色调2)) return new PAction(PCTSkill.震雷之品红3, ActionType.Gcd, ActionTargetType.Target);
            return new PAction(PCTSkill.冰冻之蓝青1, ActionType.Gcd, ActionTargetType.Target);
        }
        
        // 不在反转状态
        if (!isReversed)
        {
            if(player.HasStatus(PCTBuff.以太色调)) return new PAction(PCTSkill.疾风之绿2, ActionType.Gcd, ActionTargetType.Target);
            if(player.HasStatus(PCTBuff.以太色调2)) return new PAction(PCTSkill.流水之蓝3, ActionType.Gcd, ActionTargetType.Target);
            return new PAction(PCTSkill.火炎之红1, ActionType.Gcd, ActionTargetType.Target);
        }
        
        // 在反转状态
        if(player.HasStatus(PCTBuff.以太色调)) return new PAction(PCTSkill.飞石之纯黄2, ActionType.Gcd, ActionTargetType.Target);
        if(player.HasStatus(PCTBuff.以太色调2)) return new PAction(PCTSkill.闪雷之品红3, ActionType.Gcd, ActionTargetType.Target);
        return new PAction(PCTSkill.冰结之蓝青1, ActionType.Gcd, ActionTargetType.Target);
    }

    public static PAction GetHammerGcd()
    {
        var player = Core.Me;
        if (player.Level >= 86)
        {
            return new PAction(PCTSkill.重锤敲章1.GetAdjustedActionId(), ActionType.Gcd, ActionTargetType.Target);
        }
        return new PAction(PCTSkill.重锤敲章1, ActionType.Gcd, ActionTargetType.Target);
    }

    public static uint GetCurrentReverseCharge()
    {
        var player = Core.Me;
        var pallete = JobGaugeHelper.PCT.PalleteGauge;
        uint charge = 0;
        var hasStatus = player.HasStatus(PCTBuff.减色混合预备);
        
        if (pallete < 50 && !hasStatus) return charge;
        
        if (hasStatus)
        {
            charge = (uint)pallete / 50 + 1; // 50为一层
        }
        else
        {
            charge = (uint)pallete / 50;
        }
        return charge;
    }
    
    /// <summary>
    /// 生物画画了吗？
    /// </summary>
    /// <returns>bool</returns>
    public static bool IsCreatureDrawn()
    {
        return JobGaugeHelper.PCT.CreatureMotifDrawn;
    }
    
    /// <summary>
    /// 武器画画了吗？
    /// </summary>
    /// <returns>bool</returns>
    public static bool IsWeaponDrawn()
    {
        return JobGaugeHelper.PCT.WeaponMotifDrawn;
    }
    
    /// <summary>
    /// 风景画画了吗？
    /// </summary>
    /// <returns>bool</returns>
    public static bool IsLandscapeDrawn()
    {
        return JobGaugeHelper.PCT.LandscapeMotifDrawn;
    }
    
    /// <summary>
    /// 莫古力激流好了吗？
    /// </summary>
    /// <returns>bool</returns>
    public static bool IsMoogleCannonReady()
    {
        return JobGaugeHelper.PCT.MooglePortraitReady;
    }
    
    /// <summary>
    /// 马蒂恩惩罚好了吗？
    /// </summary>
    /// <returns>bool</returns>
    public static bool IsMadeenSmReady()
    {
        return JobGaugeHelper.PCT.MadeenPortraitReady;
    }
    
    public static float GetCurrentCreatureCharge()
    {
        var player = Core.Me;
        float MaxCharges = 3f;  // 最大层数
        float PerCharge  = 40f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = PCTSkill.动物构想.GetActionCooldown();
        
        // 等级适配
        if (player.Level < 96)
        {
            MaxCharges = 2f;
            cdToFull -= 1 * PerCharge;
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

    public static Creatures GetCurrentCreature()
    {
        var f = JobGaugeHelper.PCT.CreatureFlags;
        if (f.HasFlag(CreatureFlags.Pom))
        {
            return Creatures.Pom;
        }

        if (f.HasFlag(CreatureFlags.Wings))
        {
            return Creatures.Wings;
        }

        if (f.HasFlag(CreatureFlags.Claw))
        {
            return Creatures.Claw;
        }

        return Creatures.None;
    }
    
    public static PAction GetCurrentCreatureImage()
    {
        return new PAction(PCTSkill.动物构想.GetAdjustedActionId(), ActionType.OffGcd, ActionTargetType.Target);
    }

    public static bool IsWillMoveOrMoved()
    {
        return MoveManager.IsLocalPlayerMoving || GameData.WillMove;
    }
}
