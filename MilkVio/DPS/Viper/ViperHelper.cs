using Dalamud.Game.ClientState.JobGauge.Enums;
using PromeRotation.Core;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper;

public static class ViperHelper
{
    public static bool IsIn强碎灵()
    {
        return JobGaugeHelper.VPR.蛇剑连状态 == DreadCombo.Dreadwinder || JobGaugeHelper.VPR.蛇剑连状态 == DreadCombo.SwiftskinsCoil || JobGaugeHelper.VPR.蛇剑连状态 == DreadCombo.HuntersCoil;
    }

    public static bool IsIn附体()
    {
        var me = Core.Me;
        return me.HasStatus(ViperBuff.祖灵降临);
    }

    public static bool IsInViper120()
    {
        return GameData.IsInPure120() || ViperSkill.蛇灵气.GetActionCooldown() >= 98;
    }
    public static uint 祖灵层数()
    {
        var 灵力值 = JobGaugeHelper.VPR.灵力值;

        var stacks = (uint)(灵力值 / 50);

        if (Core.Me.HasStatus(ViperBuff.祖灵降临预备))
            stacks++;

        return stacks;
    }
    
    public static bool Is锐牙buff快到期()
    {
        const float 阈值 = 8f;
        var player = Core.Me;

        uint[] buffs =
        {
            ViperBuff.咬噬锐牙左12,
            ViperBuff.穿裂锐牙右12,
            ViperBuff.侧击锐牙绿左3,
            ViperBuff.侧裂锐牙绿右3,
            ViperBuff.背击锐牙红左3,
            ViperBuff.背裂锐牙红右3,
        };

        foreach (var buffId in buffs)
        {
            var leftTime = player.GetStatusLeftTime(buffId);
            if (leftTime != 0 && leftTime < 阈值)
                return true;
        }

        return false;
    }

    public static float 强碎灵层数()
    {
        var player = Core.Me;
        float MaxCharges = 2f;  // 最大层数
        float PerCharge  = 40f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = ViperSkill.强碎灵蛇.GetActionCooldown();
        
        // 等级适配
        if (player.Level < 65)
        {
            MaxCharges = 0f;
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
