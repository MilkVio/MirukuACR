using PromeRotation.Extensions;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper;

public static class ReaperHelper
{
    public static bool IsIn附体()
    {
        var me = Core.Me;
        if (me == null) return false;
        return me.HasStatus(ReaperBuff.夜游魂衣Buff);
    }
    
    public static bool IsIn妖异之镰()
    {
        var me = Core.Me;
        if (me == null) return false;
        return me.HasStatus(ReaperBuff.处刑人Buff) || me.HasStatus(ReaperBuff.妖异之镰Buff) ;
    }
    
    public static float 灵魂切层数()
    {
        var player = Core.Me;
        float MaxCharges = 2f;  // 最大层数
        float PerCharge  = 30f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = ReaperSkill.灵魂切割.GetActionCooldown();
        
        // 等级适配
        if (player.Level < 60)
        {
            MaxCharges = 0f;
        }
        // 等级适配
        if (player.Level < 78)
        {
            MaxCharges = 1f;
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
