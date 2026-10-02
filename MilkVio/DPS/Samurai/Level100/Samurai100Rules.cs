using MilkVio.DPS.Samurai.SAMData;
using PromeRotation.Data;

namespace MilkVio.DPS.Samurai.Level100;

internal static class Samurai100Rules
{
    // 强制垫刀优先；延迟回返在远离时放行。
    public static bool TryTsubameControl(Samurai100State s, out bool use, out string reason)
    {
        use = false; reason = "";
        if ((!s.ForcePadding && !s.DelayReturn) || s.ReturnLeft <= 0) return false;
        var mode = s.ForcePadding ? "强制垫刀" : "延迟回返";
        var ready = s.GcdLeft + Samurai100Helper.EffectMargin;
        if (s.ReturnLeft <= ready + s.Gcd)
        { use = true; reason = $"{mode}：回返将到期"; return true; }
        if (!s.ForcePadding && s.Distance > s.MeleeRange)
        { use = true; reason = "延迟回返：基础连打不到，放行回返"; return true; }
        var canCast = !s.Moving && s.Distance <= s.IaiRange && s.Moon > ready + s.Cast;
        if (s.SenCount == 3 && SafePadding(s, true) == 0 && (s.ForcePadding || canCast))
        {
            use = true;
            reason = s.MirrorStacks > 0 || s.Tendo > 0 && s.Tendo <= ready + 3 * s.Gcd + s.Cast
                ? $"{mode}：保护明镜或天道，先清旧回返" : $"{mode}：垫刀已满，下次居合前清回返";
            return true;
        }
        // 紧急奥义要连续占两G，不能把旧回返留过期。
        if (!s.ForcePadding && canCast && s.UseOgi && s.Ogi > ready + s.OgiCast &&
            s.Ogi <= ready + 2 * s.Gcd + s.OgiCast && s.ReturnLeft <= ready + 2 * s.Gcd)
        { use = true; reason = "延迟回返：奥义衔接期间回返将到期"; return true; }
        reason = $"{mode}：保留回返";
        return true;
    }

    public static bool HoldTsubame(Samurai100State s) => TryTsubameControl(s, out var use, out _) && !use;

    public static bool PaddingGcd(Samurai100State s, out uint action)
    {
        action = 0;
        if ((!s.ForcePadding && !(s.DelayReturn && s.ReturnLeft > 0)) || s.SenCount != 3) return false;
        if (s.ReturnLeft > 0 && !HoldTsubame(s)) return false;
        var filler = SafePadding(s, true);
        if (filler == 0) return false;
        if (s.Distance > s.MeleeRange) return true;
        action = filler;
        return true;
    }

    // 前两刀不取闪，也不消耗明镜；不重开已经垫好的连击。
    public static uint SafePadding(Samurai100State s, bool both = false)
    {
        var combo = s.ComboLeft > s.GcdLeft + Samurai100Helper.EffectMargin ? s.Combo : 0;
        if (s.MirrorStacks > 0 || combo == SAMSkill.阵风 || combo == SAMSkill.士风) return 0;
        var fillers = both && combo != SAMSkill.晓风 ? 2 : 1;
        var untilIai = s.GcdLeft + (fillers + (s.ReturnLeft > 0 ? 1 : 0)) * s.Gcd + s.Cast + Samurai100Helper.EffectMargin;
        if (s.Tendo > 0 && s.Tendo <= untilIai) return 0;
        return combo == SAMSkill.晓风 ? s.Moon <= s.Flower ? SAMSkill.阵风 : SAMSkill.士风 : SAMSkill.晓风;
    }

    // 再垫一刀会断花，就在本G开始读条。
    public static bool HiganbanaDue(Samurai100State s) => s.UseDot && s.SenCount == 1 &&
        s.Dot <= s.GcdLeft + s.Gcd + s.Cast + Samurai100Helper.EffectMargin;

    // 普通覆盖最多5秒；已有明镜将过期时先恢复循环。
    public static bool CanRefreshHiganbana(Samurai100State s) =>
        s.Dot <= s.GcdLeft + s.Cast + Samurai100Helper.EffectMargin + 5 ||
        s.MirrorStacks > 0 && s.MirrorLeft <= s.GcdLeft + (s.MirrorStacks + 1) * s.Gcd;

    // 开场先兑现富余的一层，仍给首个120留下两层的准备余量。
    public static bool PreferOpeningMirror(Samurai100State s) => s.BattleTime >= 0 && s.BattleTime < 60 &&
        s.UseIki && s.IkiCd > 55 && s.MirrorCharges - 1 + s.IkiCd / 55 >= 2;

    public static uint WaitingIaijutsu(Samurai100State s, bool keepSen)
    {
        var ready = s.GcdLeft + Samurai100Helper.EffectMargin;
        var lastCombo = s.ComboLeft > ready && (s.Combo == SAMSkill.阵风 || s.Combo == SAMSkill.士风);
        var mirror = s.MirrorStacks > 0 && s.MirrorLeft > ready;
        if (!lastCombo && !mirror) return 0;
        if (s.SenCount == 3 && (s.Moon > ready + s.Cast ||
            s.Tendo > ready + s.Cast && s.Tendo <= ready + 2 * s.Gcd + s.Cast)) return SAMSkill.纷乱雪月花;
        if (s.SenCount == 1 && s.UseDot && keepSen && s.Moon > ready + s.Cast) return SAMSkill.彼岸花;
        return 0;
    }

    public static string WaitReason(Samurai100State s)
    {
        if (s.Distance > s.IaiRange) return "等待居合距离";
        if (s.Moving) return "等待站定";
        if (s.SenCount == 1) return "等待彼岸花时机";
        return "保留连击，等待居合";
    }

    public static bool ReturnBeforeFlower(Samurai100State s)
    {
        return !s.UseDot || s.Dot + s.FlowerDelay > s.GcdLeft + (Samurai100Projection.ToFlower(s) + 1) * s.Gcd +
            s.Cast + Samurai100Helper.EffectMargin;
    }

    public static uint ChooseMoonFlower(Samurai100State s)
    {
        var needMoon = (s.Sen & 2) == 0;
        var needFlower = (s.Sen & 4) == 0;
        if (!needMoon) return needFlower ? SAMSkill.花车 : 0;
        if (!needFlower) return SAMSkill.月光;

        // 先走另一条路，途中大招也要给续增益留时间。
        var extra = s.ReturnLeft > 0 ? 1 : 0;
        if (s.OgiReturn) extra++;
        else if (s.UseOgi && !s.Moving && s.Ogi > s.GcdLeft + s.OgiCast) extra += 2;
        var mirror = s.MirrorStacks > 0;
        if (mirror && s.UseDot && s.SenCount == 0 && s.Dot <= s.GcdLeft + (extra + 3) * s.Gcd + s.Cast) extra++;
        var nextMirror = s.MirrorStacks > 1 && s.MirrorLeft > s.GcdLeft + (extra + 1) * s.Gcd + Samurai100Helper.EffectMargin;
        var otherGcds = 3;
        if (mirror) otherGcds = nextMirror ? 1 : 2;
        var otherBuffAt = s.GcdLeft + (extra + otherGcds) * s.Gcd + Samurai100Helper.EffectMargin;
        if (s.Moon <= otherBuffAt || s.Flower <= otherBuffAt)
            return s.Moon <= s.Flower ? SAMSkill.月光 : SAMSkill.花车;

        var positionalAt = s.GcdLeft + (mirror ? 0 : s.Gcd) + Samurai100Helper.EffectMargin;
        // 只用当前身位选近处的分支，不假定整段爆发都站同一侧。
        if (s.NeedsPosition && s.Time < s.Gcd && s.TrueNorth <= positionalAt)
        {
            if (s.Position == Positional.Flank) return SAMSkill.花车;
            if (s.Position == Positional.Rear) return SAMSkill.月光;
        }
        return s.Moon <= s.Flower ? SAMSkill.月光 : SAMSkill.花车;
    }

    public static bool CanIkishoten(Samurai100State s, out string reason)
    {
        reason = "意气补充剑气";
        if (!s.UseIki || s.IkiCd > 0) { reason = "意气未就绪或QT暂停"; return false; }
        if (s.Ogi > 0 || s.OgiReturn || s.Zanshin > 0) { reason = "先兑现已有预备"; return false; }
        if (s.Distance > s.OgiRange) { reason = "接近目标后再开意气"; return false; }
        if (!s.UseShinten) { reason = "震天关闭，意气好了就用"; return true; }
        if (s.Kenki > 50) { reason = "先腾出50剑气空间"; return false; }
        var after = s; after.IkiCd = 120; after.Ogi = after.Zanshin = 30;
        if (!CanSpendAfterIkishoten(s) && s.Kenki >= 25 && s.Distance <= s.MeleeRange &&
            s.Kenki + NextGain(after) + (s.Eye > 0 ? 10 : 0) > 50)
        { reason = "先泄剑气，给下一刀留空间"; return false; }
        return true;
    }

    // 每次从冷却和资源判断，不保存120轮次。
    public static float SelfWindow(Samurai100State s)
    {
        if (s.BattleTime >= 0 && s.BattleTime < 25) return 25 - s.BattleTime;
        var usedAt = s.BattleTime - (120 - s.IkiCd);
        if (s.BattleTime >= 25 && usedAt >= 0 && usedAt < 25) return 0;
        return s.IkiCd > 90 && s.IkiCd <= 120 ? s.IkiCd - 90 : 0;
    }

    public static bool HasResources(Samurai100State s) => s.ReturnLeft > 0 || s.OgiReturn ||
        s.UseOgi && s.Ogi > 0 || s.UseZanshin && s.Zanshin > 0 || s.Tendo > 0 ||
        s.MirrorStacks > 0 || s.SenCount >= 2 || s.UseShinten && s.Kenki >= 25;

    public static bool Spending(Samurai100State s) => SelfWindow(s) > 0 && HasResources(s);

    // 本轮最后一次居合刚生成的回返，不因30秒边界重新扣住。
    public static bool FinishingReturn(Samurai100State s)
    {
        var elapsed = 120 - s.IkiCd;
        return s.BattleTime >= 60 && elapsed >= 30 && elapsed < 30 + s.Gcd &&
            s.ReturnLeft >= 30 - (elapsed - 30) - Samurai100Helper.EffectMargin;
    }

    public static float PrepareSeconds(Samurai100State s) =>
        Math.Clamp((Samurai100Projection.ToThree(s) + 1) * s.Gcd, 4 * s.Gcd, 8 * s.Gcd);

    public static bool Preparing(Samurai100State s) => s.UseIki && s.IkiCd <= PrepareSeconds(s) &&
        s.Ogi <= 0 && !s.OgiReturn && s.Zanshin <= 0;

    public static string Phase(Samurai100State s)
    {
        if (Spending(s)) return s.BattleTime < 25 ? "开场集中兑现" : "当前120兑现";
        if (Preparing(s)) return "准备意气";
        if (s.OgiReturn || s.UseOgi && s.Ogi > 0 || s.UseZanshin && s.Zanshin > 0) return "剩余预备收尾";
        return "平稳";
    }

    public static int KenkiGain(uint action) => action == SAMSkill.雪风 ? 15 :
        action == SAMSkill.月光 || action == SAMSkill.花车 ? 10 :
        action == SAMSkill.晓风 || action == SAMSkill.阵风 || action == SAMSkill.士风 ? 5 : 0;

    public static int NextGain(Samurai100State s) => KenkiGain(Samurai100Projection.NextResourceGcd(s));

    public static bool CanSpendAfterIkishoten(Samurai100State s) =>
        s.MaxWeaves > 1 && s.GcdLeft >= 2 * Samurai100Projection.AbilityLock &&
        (s.UseZanshin && s.ZanshinCd <= Samurai100Projection.AbilityLock && s.Distance <= s.OgiRange ||
         s.UseSenei && s.SeneiCd <= Samurai100Projection.AbilityLock && s.Moon > 0 && s.Distance <= s.MeleeRange);

    public static int Reserve(Samurai100State s)
    {
        // 暂停技能仍保留重开预算；远期闪影由途中连击供气。
        var reserve = s.Zanshin > 0 ? 50 : 0;
        reserve += SeneiReserve(s);
        return reserve;
    }

    public static int SeneiReserve(Samurai100State s) => s.SeneiCd <= 8 * s.Gcd ?
        Math.Max(0, 25 - Samurai100Projection.IncomeBefore(s, s.SeneiCd)) : 0;

    public static bool CanZanshin(Samurai100State s, bool budget = false)
    {
        if (!s.UseZanshin || s.Zanshin <= 0 || s.ZanshinCd > 0 || s.Kenki < 50 || s.Distance > s.OgiRange) return false;
        if (s.Zanshin <= s.GcdLeft + s.Gcd + Samurai100Helper.EffectMargin) return true;
        // 预算推演不递归求预算；真实选择保护闪影的25剑气。
        var reserve = budget ? s.SeneiCd <= s.Gcd ? 25 : 0 : SeneiReserve(s);
        return !s.UseSenei || s.Kenki - 50 >= reserve;
    }

    public static int BeforeIkiLimit(Samurai100State s)
    {
        return Samurai100Projection.BeforeIkishotenLimit(s) - (s.Eye > 0 ? 10 : 0);
    }

    public static bool SpendKenki(Samurai100State s, out string reason)
    {
        reason = "保留剑气";
        if (!s.UseShinten || s.ShintenCd > 0 || s.Kenki < 25 || s.Distance > s.MeleeRange) return false;
        var gain = NextGain(s);
        var eye = s.Eye > 0 ? 10 : 0;
        if (s.Kenki + gain + eye > 100)
        { reason = eye > 0 ? "为下刀和天眼通受击留空间" : "下一刀剑气将溢出"; return true; }
        var reserve = Reserve(s);
        // 意气马上补气，不能为后续闪影留气反而挡住意气。
        if (Preparing(s) && s.Kenki > BeforeIkiLimit(s) &&
            (s.IkiCd <= s.GcdLeft + s.Gcd || s.Kenki - 25 >= reserve))
        { reason = "按意气的实际插入位提前泄剑气"; return true; }
        if (s.Kenki - 25 < reserve) return false;
        if (Spending(s) || s.Potion > Samurai100Helper.EffectMargin || s.Party > Samurai100Helper.EffectMargin || s.Dump)
        { reason = s.Potion > 0 ? "药内消费剩余剑气" : "当前爆发消费剩余剑气"; return true; }
        if (Preparing(s)) { reason = "为将就绪的意气集中剑气"; return false; }
        reason = "平稳期消费多余剑气";
        return true;
    }
}
