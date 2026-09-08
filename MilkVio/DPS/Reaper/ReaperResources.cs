namespace MilkVio.DPS.Reaper;

// 只推演有限的GCD步数，不查询游戏，不修改实际量谱。
public static class ReaperResources
{
    private const float WeaveTime = 0.65f;
    private const float DeathDesignSafety = 0.25f;

    public static float WindowCircleCoverage(ReaperState s)
    {
        if (!s.WindowActive || !s.CircleQt) return 0;
        var at = Math.Max(0, s.CircleCd);
        // 环的效果生效需要时间；到截止才转好不算可用团辅。
        return Math.Clamp(s.WindowLeft - at - WeaveTime, 0, 20);
    }

    // 极短覆盖不强制攒整套资源，交给窗口收尾比较眼前能落地的技能。
    public static bool HasWindowCircle(ReaperState s) => WindowCircleCoverage(s) >= Math.Max(2 * s.Gcd, 1);

    public static bool IsWindowClosing(ReaperState s)
    {
        if (!s.WindowActive || !s.HasTiming || s.CircleLeft <= 0 && HasWindowCircle(s)) return false;
        // 已有资源及少量回资源技能需要的时间，不固定为最后20秒。
        var shrouds = s.EnshroudQt ? Math.Max(0, s.Shroud - s.GoalShroud) / 50 : 0;
        var free = s.EnshroudQt && (s.FreeEnshroud > 0 || s.SacrificeStacks > 0) ? 1 : 0;
        var reds = s.BloodQt || s.GluttonyQt ? Math.Max(0, s.Soul - s.GoalSoul) / 50 : 0;
        if (s.SliceQt && s.SliceCharges >= 1 && (s.BloodQt || s.GluttonyQt)) reds++;
        var duration = (shrouds + free) * (4 * s.ReapGcd + s.CommunioGcd)
            + (reds + (s.GluttonyQt && s.GluttonyCd <= s.Gcd ? 1 : 0)) * s.Gcd
            + (free > 0 || s.Perfectio > 0 || s.Occulta > 0 ? s.PerfectioGcd + s.Gcd : 0)
            + (s.Enshrouded > 0 ? s.ShroudFinishIn : s.Reavers * s.Gcd) + 2 * s.Gcd;
        return s.WindowLeft <= Math.Clamp(duration, 3 * s.Gcd, 60);
    }

    public static bool NeedsDesignInShroud(ReaperState s) => s.Enshrouded > 0 && s.DotQt && s.Melee
        && s.Reavers == 0 && s.HasTiming && !s.CastingCommunio
        && (!s.WindowActive || s.WindowLeft > s.GcdLeft + 2 * s.Gcd)
        && s.DeathDesign <= s.ShroudFinishIn + (s.Occulta > 0 ? s.CommunioGcd - s.CommunioCast : 0);

    public static bool AllowsHarvestMoon(ReaperState s) => s.Alive && s.HasTarget && !s.CastingCommunio
        && s.HarvestMoonQt && s.Soulsow && s.Reavers == 0
        && s.Distance <= GameData.GetCurrentAttackRange(25)
        && (s.Enshrouded > 0 ? s.CanHarvestMoonForRange
            || s.WindowActive && s.Lemure == 1 && s.WindowLeft > s.GcdLeft + 0.15f
                && s.WindowLeft <= s.GcdLeft + s.CommunioCast + 0.15f
            : !s.Melee || s.IsDump || IsWindowClosing(s));

    private static float DeathDesignWindow(ReaperState s, float time = 0, bool allowEarlyRefresh = true)
    {
        if (!s.HasTiming) return 3;
        var preparingBurst = s.CircleQt && s.CircleCd - time <= s.PreparationLead;
        var early = allowEarlyRefresh && !s.DumpQt && s.CircleLeft <= time
            && s.Enshrouded <= time && !preparingBurst;
        return (early ? 2 : 1) * s.Gcd + DeathDesignSafety;
    }

    public static bool ShouldRefreshDeathDesign(ReaperState s, bool allowEarlyRefresh = true) =>
        s.DotQt && s.Melee && s.Reavers == 0
        && (!s.WindowActive || s.WindowLeft > s.GcdLeft + s.Gcd * 2)
        && s.DeathDesign <= s.GcdLeft + DeathDesignWindow(s, allowEarlyRefresh: allowEarlyRefresh);

    public static bool EnshroudWinsRemainingBuff(ReaperState s)
    {
        if (s.CircleLeft <= 0 || !s.HasTiming) return true;
        if (s.FreeEnshroud > 0 && s.FreeEnshroud <= s.Gcd + 1) return true;
        // 只比较眼前团辅中的几招；团契打不进去时，收割和祭牲仍可能比普通消费更赚。
        float Inside(float time, float potency) => time + 0.15f < s.CircleLeft ? potency : 0;
        var enshroud = 0f;
        for (var i = 0; i < 4; i++) enshroud += Inside(s.GcdLeft + i * s.ReapGcd, i == 0 ? 580 : 640);
        enshroud += Inside(s.GcdLeft > 1.3f ? 0.65f : s.GcdLeft + 0.65f, s.Level >= 92 ? 700 : 0);
        enshroud += Inside(s.GcdLeft + s.ReapGcd + 0.65f, 280) + Inside(s.GcdLeft + 3 * s.ReapGcd + 0.65f, 280);
        enshroud += Inside(s.GcdLeft + 4 * s.ReapGcd + s.CommunioCast, 1100);
        if (s.Occulta > 0) enshroud += Inside(s.GcdLeft + 4 * s.ReapGcd + s.CommunioGcd, 1300);
        var other = 0f;
        if (s.GluttonyQt && s.GluttonyCd <= 0 && s.Soul >= 50)
            other = Inside(0, 560) + Inside(s.GcdLeft, 820) + Inside(s.GcdLeft + s.Gcd, 820);
        else if (s.BloodQt && s.Soul >= 50)
            other = Inside(0, 440) + Inside(s.GcdLeft, 620) + Inside(s.GcdLeft + s.Gcd, 500);
        return enshroud > other;
    }

    public readonly record struct GluttonyEstimate(float Time, int LostSoul, float SoulReadyAt = 0);

    private static float NextGcdAfterGluttony(ReaperState s, float gluttonyAt, float gcdAt)
    {
        var first = gcdAt;
        if (gluttonyAt >= first)
            first += (MathF.Floor((gluttonyAt - first) / s.Gcd) + 1) * s.Gcd;
        // 两次处刑之后，最早能再次使用死亡之影的时刻。
        return first + 2 * s.Gcd;
    }

    private static bool NeedsGluttonyPreparation(ReaperState s, float dot, float gcdAt, float gluttonyAt)
    {
        return s.DotQt && s.GluttonyQt && s.Level >= 76
            && gluttonyAt < gcdAt + s.Gcd + WeaveTime + DeathDesignSafety
            && dot <= NextGcdAfterGluttony(s, gluttonyAt, gcdAt) + DeathDesignSafety;
    }

    public static bool ShouldRefreshBeforeGluttony(ReaperState s)
    {
        if (!s.HasTiming || !s.Melee || s.Locked || !s.DotQt || !s.GluttonyQt || s.Level < 76
            || s.WindowActive && s.WindowLeft <= s.GcdLeft + 3 * s.Gcd
            || s.DeathDesign > 30) return false;
        var gluttonyAt = EstimateGluttony(s, false).Time;
        // 下一轮才补会错过暴食的插入位置，就用当前这个GCD准备。
        return float.IsFinite(gluttonyAt) && NeedsGluttonyPreparation(s, s.DeathDesign, s.GcdLeft, gluttonyAt);
    }

    public static GluttonyEstimate EstimateGluttony(ReaperState s, bool spendBlood, bool enshroudNow = false)
    {
        if (!s.HasTiming || !float.IsFinite(s.GluttonyCd) || s.GluttonyCd < 0)
            return new(float.PositiveInfinity, 0, float.PositiveInfinity);
        if (enshroudNow && (!s.CanEnshroud || spendBlood || s.Level < 90))
            return new(float.PositiveInfinity, 0, float.PositiveInfinity);
        var soul = s.Soul - (spendBlood ? 50 : 0);
        var soulReadyAt = soul >= 50 ? 0 : float.PositiveInfinity;
        var reavers = s.Reavers + (spendBlood ? 1 : 0);
        var charges = s.SliceCharges;
        var dot = s.DeathDesign;
        var lost = 0;
        var previousTime = 0f;
        var perfectioUsed = enshroudNow && s.Occulta > 0;
        var harvestUsed = false;
        var firstGcd = s.GcdLeft + (enshroudNow ? s.SingleDuration : 0);
        var firstWeave = enshroudNow
            ? s.GcdLeft + 4 * s.ReapGcd + (s.Occulta > 0 ? s.CommunioGcd + WeaveTime : s.CommunioCast)
            : 0;
        var readyAt = Math.Max(s.GluttonyCd, firstWeave);
        if (reavers == 0 && soul >= 50 && readyAt <= Math.Max(0, firstGcd - WeaveTime)
            && (!s.DotQt || dot > NextGcdAfterGluttony(s, readyAt, firstGcd) + DeathDesignSafety))
            return new(readyAt, 0);

        for (var i = 0; i < 64; i++)
        {
            var time = firstGcd + i * s.Gcd;
            charges = Math.Min(2, charges + (time - previousTime) / Math.Max(1, s.SliceRecast));
            previousTime = time;
            var gain = 0;
            var weave = Math.Max(s.GluttonyCd, time + WeaveTime);
            if (weave > time + s.Gcd - WeaveTime)
                weave = Math.Max(s.GluttonyCd, time + s.Gcd + WeaveTime);
            if (reavers > 0) reavers--;
            else if (!perfectioUsed && s.Perfectio > time && s.CircleLeft > time
                && s.CircleLeft - time <= Math.Max(s.Gcd, 2)) perfectioUsed = true;
            else if (s.DotQt && (dot <= time + DeathDesignWindow(s, time)
                || soul >= 50 && NeedsGluttonyPreparation(s, dot, time, weave)))
                dot = Math.Min(time + 60, Math.Max(dot, time) + 30);
            else if (!perfectioUsed && s.Perfectio > time
                && (!s.FarPerfectioQt || s.CircleLeft > time || s.Perfectio - time <= 6)) perfectioUsed = true;
            else if (!harvestUsed && s.SacrificeStacks > 0 && s.Bloodsown <= time && s.SacrificeLeft > time
                && s.FreeEnshroud <= 0 && s.Perfectio <= 0
                && (s.EnshroudQt || s.SacrificeLeft - time <= s.Gcd + 1)) harvestUsed = true;
            else if (s.SliceQt && charges >= 1 && soul <= 50) { charges--; gain = 50; }
            else gain = 10;
            lost += Math.Max(0, soul + gain - 100);
            soul = Math.Min(100, soul + gain);
            if (soul >= 50) soulReadyAt = Math.Min(soulReadyAt, time);
            if (reavers == 0 && soul >= 50 && weave <= time + s.Gcd - WeaveTime
                && (!s.DotQt || dot > NextGcdAfterGluttony(s, weave, time) + DeathDesignSafety))
                return new(weave, lost, soulReadyAt);
        }
        return new(float.PositiveInfinity, lost, soulReadyAt);
    }

    public static bool ShouldHoldBlood(ReaperState s, out string reason)
    {
        reason = "";
        if (!s.GluttonyQt || s.Level < 76 || !s.HasTiming || s.Soul < 50) return false;
        if (!float.IsFinite(s.GluttonyCd) || s.GluttonyCd < 0) return false;
        if (s.GluttonyCd <= 0) { reason = "暴食已好，先暴食"; return true; }
        var keep = EstimateGluttony(s, false);
        var spend = EstimateGluttony(s, true);
        if (!float.IsFinite(keep.Time) || !float.IsFinite(spend.Time)) return false;
        var drift = spend.Time - keep.Time;
        if (drift <= 0.25f) return false;

        // 溢出红或充能迫近时允许多占一个GCD；不是看到90红就无条件推迟暴食。
        var chargePressure = s.SliceQt && s.SliceCharges >= 1
            && (2 - s.SliceCharges) * s.SliceRecast <= keep.Time + s.Gcd;
        if ((keep.LostSoul > 0 || chargePressure) && drift <= s.Gcd + 0.25f) return false;
        reason = "留50红给即将转好的暴食";
        return true;
    }

    public readonly record struct PreparationEstimate(bool Ready, float CircleAt, int Shroud, string Reason);

    internal static bool CanSkipFirstDesign(ReaperState s, float firstReapAt, out float circleAt)
    {
        circleAt = float.PositiveInfinity;
        if (!s.HasTiming || !s.CircleQt || !s.EnshroudQt || !float.IsFinite(firstReapAt)
            || !float.IsFinite(s.CircleCd) || s.CircleCd < 0) return false;
        circleAt = Math.Max(0, Math.Max(s.CircleCd, firstReapAt + WeaveTime));
        if (circleAt + WeaveTime + 0.05f > firstReapAt + s.ReapGcd) return false;

        var harvestAt = firstReapAt + 4 * s.ReapGcd + s.CommunioGcd;
        if (harvestAt < circleAt + 6.7f) return false;
        var secondStart = harvestAt + s.Gcd;
        var communioAt = secondStart + 4 * s.ReapGcd + s.CommunioCast;
        if (communioAt + 0.15f >= circleAt + 20
            || s.WindowActive && communioAt + 0.15f >= s.WindowLeft) return false;

        var perfectioAt = secondStart + 4 * s.ReapGcd + s.CommunioGcd;
        var designAt = perfectioAt + s.PerfectioGcd;
        if (s.ComboNext != 0 && s.ComboLeft > designAt) designAt += s.Gcd;
        // 烙印要撑到收尾能续印的位置，完人是否进团辅仍只作尽力目标。
        return s.DeathDesign > designAt + DeathDesignSafety;
    }

    public static int ForecastShroud(ReaperState s, bool enshroudNow) => ForecastPreparation(s, enshroudNow).Shroud;

    public static PreparationEstimate ForecastPreparation(ReaperState s, bool enshroudNow)
    {
        if (!s.HasTiming || !float.IsFinite(s.CircleCd)) return new(false, float.PositiveInfinity, -1, "时序未读取");
        var circleDelay = s.FastCircle ? WeaveTime : s.Gcd / 2;
        var horizon = Math.Clamp(s.CircleCd + circleDelay + 0.25f, 0, 125);
        var green = s.Shroud - (enshroudNow && s.FreeEnshroud <= 0 ? 50 : 0);
        var soul = s.Soul;
        var charges = s.SliceCharges;
        var pending = s.Reavers;
        var glutReady = s.GluttonyCd;
        var dot = s.DeathDesign;
        var previousTime = 0f;
        var occupied = enshroudNow ? s.SingleDuration : s.Enshrouded > 0
            ? Math.Max(0, s.Lemure - 1) * s.ReapGcd + s.CommunioGcd + (s.Occulta > 0 ? s.PerfectioGcd : 0)
            : s.Perfectio > 0 ? s.PerfectioGcd : 0;
        var time = s.GcdLeft + occupied;
        var circleAt = float.PositiveInfinity;
        for (var i = 0; i < 96 && time + 2 * s.Gcd + WeaveTime <= horizon; i++, time += s.Gcd)
        {
            if (time + 2 * s.Gcd + s.ReapGcd + circleDelay > horizon && (green < 50 || pending > 0)) break;
            if (green >= 50 && pending == 0)
            {
                var prepareAt = time;
                // 窗口截止检查还要容纳神秘环错过插入位置后，增加的准备GCD。
                if (s.WindowActive)
                {
                    var lastWeave = time + 3 * s.Gcd + s.ReapGcd - WeaveTime;
                    prepareAt += Math.Max(0, MathF.Ceiling((s.CircleCd - lastWeave) / s.Gcd)) * s.Gcd;
                }
                circleAt = Math.Max(s.CircleCd, prepareAt + 2 * s.Gcd + s.ReapGcd + circleDelay);
                var finishAt = prepareAt + 4 * s.Gcd + 8 * s.ReapGcd + s.CommunioGcd + s.CommunioCast;
                var designUntil = dot;
                if (s.DotQt && dot < prepareAt + 10)
                    designUntil = Math.Min(prepareAt + 60, Math.Max(dot, prepareAt) + 30);
                if (CanSkipFirstDesign(s with { DeathDesign = designUntil }, prepareAt + 2 * s.Gcd, out var earlyCircle))
                {
                    circleAt = earlyCircle;
                    finishAt -= s.Gcd;
                }
                var fits = circleAt <= horizon && (!s.WindowActive || finishAt + 0.15f < s.WindowLeft);
                return new(fits, circleAt, green, fits ? "完整120准备可达" : "回绿后没有完整120准备位置");
            }
            charges = Math.Min(2, charges + (time - previousTime) / Math.Max(1, s.SliceRecast));
            previousTime = time;
            var weave = Math.Max(glutReady, time + WeaveTime);
            if (weave > time + s.Gcd - WeaveTime)
                weave = Math.Max(glutReady, time + s.Gcd + WeaveTime);
            var gluttonyBeforeGcd = s.GluttonyQt && glutReady <= time - WeaveTime
                && (!s.DotQt || dot > NextGcdAfterGluttony(s, time - WeaveTime, time) + DeathDesignSafety);
            var refreshDot = s.DotQt && (dot <= time + DeathDesignWindow(s, time)
                || !gluttonyBeforeGcd && soul >= 50 && NeedsGluttonyPreparation(s, dot, time, weave));
            // 能力技要在这个GCD之前有插入位置，产生的绿则要等身位GCD真正占完。
            if (pending == 0 && soul >= 50 && time >= WeaveTime && !refreshDot)
            {
                if (gluttonyBeforeGcd)
                {
                    soul -= 50; pending = 2; glutReady = time - WeaveTime + 60;
                }
                else if (s.BloodQt && (!s.GluttonyQt || glutReady - time > 2 * s.Gcd || soul >= 90))
                {
                    soul -= 50; pending = 1;
                }
            }
            if (pending > 0) { pending--; green = Math.Min(100, green + 10); }
            else if (refreshDot) dot = Math.Min(time + 60, Math.Max(dot, time) + 30);
            else if (s.SliceQt && charges >= 1 && soul <= 50) { charges--; soul += 50; }
            else soul = Math.Min(100, soul + 10);
        }
        return new(false, circleAt, Math.Max(0, green), "魂衣或互锁结束晚于120准备");
    }

    public static bool ShouldHoldEnshroud(ReaperState s, out string reason)
    {
        reason = "";
        if (!s.HasTiming || s.CircleLeft > 0 || s.DumpQt) return false;
        if (IsWindowClosing(s)) return false;
        var reserveCircle = s.CircleQt && (!s.WindowActive || HasWindowCircle(s));
        if (s.FreeEnshroud > 0 && s.FreeEnshroud <= s.SingleDuration + s.Gcd) return false;
        if (reserveCircle && s.CircleCd <= s.GcdLeft + s.SingleDuration + s.PreparationLead)
        {
            reason = "为120留出准备时间，不再开普通附体";
            return true;
        }
        if (reserveCircle && s.FreeEnshroud <= 0 && !ForecastPreparation(s with { WindowActive = false }, true).Ready)
        {
            reason = "保留魂衣，保证下次120的50绿和完整准备";
            return true;
        }
        if (s.GluttonyQt && s.Level >= 76)
        {
            var without = EstimateGluttony(s, false);
            var withEnshroud = EstimateGluttony(s, false, enshroudNow: true);
            var avoidSoon = s.GluttonyCd < 13 && without.Time <= 13;
            var drift = withEnshroud.Time - without.Time;
            var needsRed = s.Soul < 50 && float.IsFinite(withEnshroud.Time) && float.IsFinite(without.Time)
                && withEnshroud.SoulReadyAt + WeaveTime > s.GluttonyCd + 0.25f && drift > 0.25f;
            if (!avoidSoon && !needsRed) return false;

            // 等暴食及两次处刑之后，不能挤掉这次附体或让预备/魂衣损失。
            var afterGluttony = NextGcdAfterGluttony(s, without.Time, s.GcdLeft);
            var entry = afterGluttony - s.Gcd + WeaveTime;
            var losingFree = s.FreeEnshroud > 0 && (s.FreeEnshroud <= entry + 0.5f
                || s.Occulta > 0 && s.Occulta <= afterGluttony + 4 * s.ReapGcd + s.CommunioCast + 0.5f);
            var losingWindow = reserveCircle && afterGluttony + s.SingleDuration + s.PreparationLead >= s.CircleCd;
            if (losingFree || float.IsFinite(drift) && drift <= s.Gcd + 0.25f
                && (s.FreeEnshroud <= 0 && s.Shroud > 80 || losingWindow))
            {
                reason = "优先保住附体预备或使用窗口";
                return false;
            }
            reason = avoidSoon ? "普通附体避开即将转好的暴食" : "普通附体后回红不及，先准备暴食";
            return true;
        }
        return false;
    }
}
