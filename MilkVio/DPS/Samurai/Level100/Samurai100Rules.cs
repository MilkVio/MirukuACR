using MilkVio.DPS.Samurai.SAMData;
using PromeRotation.Data;

namespace MilkVio.DPS.Samurai.Level100;

internal static class Samurai100Rules
{
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
        if (s.UseShoha && s.Meditation == 3 && s.ShohaCd <= 0 && s.Distance <= s.ShohaRange)
        { reason = "先用照破，防止剑压溢出"; return false; }
        if (!s.UseShinten) { reason = "震天关闭，意气好了就用"; return true; }
        if (s.Kenki > 50) { reason = "先腾出50剑气空间"; return false; }
        var canSpendAfter = s.MaxWeaves > 1 && s.GcdLeft >= 2 * Samurai100Projection.AbilityLock &&
            (s.UseZanshin && s.ZanshinCd <= Samurai100Projection.AbilityLock ||
             s.UseSenei && s.SeneiCd <= Samurai100Projection.AbilityLock && s.Moon > 0 && s.Distance <= s.MeleeRange);
        if (!canSpendAfter && s.Kenki >= 25 && s.Distance <= s.MeleeRange && s.Kenki + NextGain(s) + (s.Eye > 0 ? 10 : 0) > 50)
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

    public static int Reserve(Samurai100State s)
    {
        // 暂停技能仍保留重开预算；远期闪影由途中连击供气。
        var reserve = s.Zanshin > 0 ? 50 : 0;
        if (s.SeneiCd <= 8 * s.Gcd)
            reserve += Math.Max(0, 25 - Samurai100Projection.IncomeBefore(s, s.SeneiCd));
        return reserve;
    }

    public static int BeforeIkiLimit(Samurai100State s)
    {
        var income = Samurai100Projection.IncomeBefore(s, Math.Max(s.GcdLeft, s.IkiCd) + Samurai100Helper.EffectMargin, false);
        if (s.UseSenei && s.SeneiCd <= s.IkiCd && s.Moon > s.SeneiCd &&
            s.Distance <= s.MeleeRange && s.Kenki + income >= 25) income = Math.Max(0, income - 25);
        return 50 - income - (s.Eye > 0 ? 10 : 0);
    }

    public static bool SpendKenki(Samurai100State s, out string reason)
    {
        reason = "保留剑气";
        if (!s.UseShinten || s.ShintenCd > 0 || s.Kenki < 25 || s.Distance > s.MeleeRange) return false;
        var gain = NextGain(s);
        var eye = s.Eye > 0 ? 10 : 0;
        if (s.Kenki + gain + eye > 100)
        { reason = eye > 0 ? "为下刀和天眼通受击留空间" : "下一刀剑气将溢出"; return true; }
        if (s.MaxWeaves == 1 && s.Kenki > 85)
        { reason = "单插提前留剑气空间"; return true; }
        var next = Samurai100Projection.NextResourceGcd(s);
        if (s.Kenki > 85 && s.Meditation == 2 && s.UseShoha &&
            (next == SAMSkill.纷乱雪月花 || next == SAMSkill.彼岸花))
        { reason = "居合后要照破，提前留剑气空间"; return true; }
        if (s.Kenki - 25 < Reserve(s)) return false;
        if (Preparing(s) && s.Kenki > BeforeIkiLimit(s))
        { reason = "按近期收入腾出意气空间"; return true; }
        if (Spending(s) || s.Potion > Samurai100Helper.EffectMargin || s.Party > Samurai100Helper.EffectMargin || s.Dump)
        { reason = s.Potion > 0 ? "药内消费剩余剑气" : "当前爆发消费剩余剑气"; return true; }
        if (Preparing(s)) { reason = "为将就绪的意气集中剑气"; return false; }
        reason = "平稳期消费多余剑气";
        return true;
    }
}
