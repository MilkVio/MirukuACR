using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper;

internal enum ReaperBurstRoute { 当前安排, 大丰收优先, 付费附体优先, 暴食衔接大丰收 }

// 非标准120只比较几种消费顺序；具体出招仍由当前量谱、互锁和实际复唱决定。
internal static class ReaperBurstRecovery
{
    // 最多额外卡GCD 0.6秒，包含解锁同步宽限；这是经验折中值，不是游戏固定机制。
    internal const float HarvestMaxExtraWaitSeconds = 0.6f;

    internal static bool IsActive(ReaperState s) => !s.IsDump && s.HasTiming && s.Level >= 100
        && (s.CircleLeft > 0 || s.CircleQt && s.CircleCd <= s.PreparationLead);

    internal static bool PendingHarvest(ReaperState s) => s.CircleQt && s.SacrificeStacks > 0
        && s.SacrificeLeft > s.Bloodsown + 0.15f && s.FreeEnshroud <= 0 && s.Perfectio <= 0 && s.Occulta <= 0;

    internal static float HarvestAt(ReaperState s) => s.Bloodsown <= s.GcdLeft + HarvestMaxExtraWaitSeconds
        ? Math.Max(s.GcdLeft, s.Bloodsown)
        : s.GcdLeft + MathF.Ceiling(Math.Max(0, s.Bloodsown - s.GcdLeft) / s.Gcd) * s.Gcd;

    internal static bool HarvestNextGcd(ReaperState s) => s.HasTiming && !s.Locked
        && float.IsFinite(s.Gcd)
        && float.IsFinite(s.GcdLeft) && s.GcdLeft >= 0 && s.GcdLeft <= s.Gcd
        && float.IsFinite(s.Bloodsown) && s.Bloodsown >= 0 && s.Bloodsown <= s.GcdLeft + HarvestMaxExtraWaitSeconds
        && !s.ComboAtRisk(s.Gcd + Math.Max(0, s.Bloodsown - s.GcdLeft))
        && s.SacrificeLeft > Math.Max(s.GcdLeft, s.Bloodsown) + 0.15f
        && (s with { Bloodsown = 0 }).CanHarvest;

    internal static bool PrioritizeHarvestReward(ReaperState s)
    {
        if (s.IsDump || !s.Alive || !s.HasTarget || !s.HasTiming || s.Level < 100 || s.Locked)
            return false;
        var free = s.EnshroudQt && s.FreeEnshroud > 0 && s.Perfectio <= 0;
        if (!free && !HarvestNextGcd(s)) return false;
        if (!s.WindowActive) return true;
        // 附体关闭时只兑现大丰收本身，不预支后续附体的收益或占用时间。
        if (!s.EnshroudQt) return HarvestAt(s) + 0.65f < s.WindowLeft;
        // 自己的神秘环结束不是输出截止；只有真实窗口不足时才比较眼前几招。
        var firstReap = free ? s.GcdLeft + (s.GcdLeft > 0.65f ? 0 : s.Gcd) : HarvestAt(s) + s.Gcd;
        var enshroudAt = Math.Max(s.EnshroudCd, Math.Max(0, 0.65f - s.GcdElapsed));
        if (enshroudAt + 0.65f > firstReap)
            firstReap += MathF.Ceiling((enshroudAt + 0.65f - firstReap) / s.Gcd) * s.Gcd;
        var communioAt = firstReap + 4 * s.ReapGcd + s.CommunioCast + 0.65f;
        return communioAt < s.WindowLeft;
    }

    internal static (uint Gcd, uint Off) Apply(ReaperState s, ReaperBurstRoute route, uint gcd, uint off)
    {
        if (route == ReaperBurstRoute.当前安排 || !IsActive(s) || s.Locked || PrioritizeHarvestReward(s)) return (gcd, off);
        // 神秘环按时开；当前连击和即将掉落的烙印也不能被候选路线越过。
        var protect = s.ComboAtRisk(s.Gcd) || gcd == ReaperSkill.死亡之影 && s.DeathDesign <= s.GcdLeft + s.Gcd;
        if (off == ReaperSkill.神秘环 || protect) return (gcd, off);

        if (route == ReaperBurstRoute.付费附体优先)
        {
            if (s.FreeEnshroud <= 0 && s.CanEnshroud && ReaperProjection.CanUse(s, ReaperSkill.夜游魂衣, true)
                && gcd is not (ReaperSkill.死亡之影 or ReaperSkill.完人))
                return (gcd == ReaperSkill.大丰收 ? 0 : gcd, ReaperSkill.夜游魂衣);
            return (gcd, off);
        }

        if (route == ReaperBurstRoute.暴食衔接大丰收 && s.GluttonyQt && s.GluttonyCd <= s.Gcd
            && s.FreeEnshroud <= 0 && s.Perfectio <= 0 && !s.ComboAtRisk(2 * s.Gcd))
        {
            if (ReaperProjection.CanUse(s, ReaperSkill.暴食, true)) return (gcd, ReaperSkill.暴食);
            if (s.Soul < 50 && s.SliceQt && s.SliceCharges >= 1)
                return (ReaperSkill.灵魂切割, 0);
        }

        if (!PendingHarvest(s)) return (gcd, off);
        if (HarvestNextGcd(s)) return (ReaperSkill.大丰收, 0);

        // 只允许能够在大丰收那格之前打完的红消费；GCD照常填充，不空转等解锁。
        var harvestAt = HarvestAt(s);
        if (off == ReaperSkill.夜游魂衣
            || off == ReaperSkill.暴食 && s.GcdLeft + 2 * s.Gcd > harvestAt + 0.01f
            || off == ReaperSkill.隐匿挥割 && s.GcdLeft + s.Gcd > harvestAt + 0.01f) off = 0;
        return (gcd, off);
    }

    internal static ReaperBurstRoute AfterAction(ReaperBurstRoute route, uint id) => id switch
    {
        ReaperSkill.大丰收 => ReaperBurstRoute.当前安排,
        ReaperSkill.夜游魂衣 when route == ReaperBurstRoute.付费附体优先 => ReaperBurstRoute.当前安排,
        ReaperSkill.暴食 when route == ReaperBurstRoute.暴食衔接大丰收 => ReaperBurstRoute.大丰收优先,
        _ => route
    };
}
