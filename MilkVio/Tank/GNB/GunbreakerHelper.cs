using PromeRotation.Extensions;
using MilkVio.Tank.GNB.GNBData;


namespace MilkVio.Tank.GNB;

public static class GunbreakerHelper
{
    public static float GetLieFangStack()
    {
        float MaxCharges = 2f;  // 最大层数
        float PerCharge  = 30f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = GNBSkill.烈牙.GetActionCooldown();
        
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
