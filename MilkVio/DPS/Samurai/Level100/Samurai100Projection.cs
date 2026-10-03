using MilkVio.DPS.Samurai.SAMData;
using PromeRotation.Data;

namespace MilkVio.DPS.Samurai.Level100;

// 一份当前资源；推算只改副本。
internal record struct Samurai100State
{
    public uint Player, Target, Combo;
    public int Sen, Kenki, Meditation, MirrorStacks, MaxWeaves;
    public float Gcd, Cast, OgiCast, GcdLeft, ComboLeft, Moon, Flower, Dot;
    public float MirrorCharges, MirrorLeft, Tendo, ReturnLeft, Ogi, Zanshin;
    public float IkiCd, SeneiCd, ShohaCd, ShintenCd, ZanshinCd, Potion, Party, Distance;
    public float MeleeRange, IaiRange, OgiRange, ShohaRange, WindowStart, PotionReadyAt;
    public float BattleTime, Eye, TrueNorth;
    public Positional Position;
    public bool NeedsPosition;
    public bool ReturnTendo, OgiReturn, Moving, Casting;
    public bool UseDot, UseMirror, UseIki, UseSenei, UseOgi, UseZanshin, UseShinten, UseShoha, Immediate, Dump;
    public bool ReturnIsOld;
    public bool ForcePadding, DelayReturn, MirrorRequested, MirrorForced, AutoMirror, TargetUnavailable;
    public float Time;
    // 仅本次推算使用，不保存到战斗状态。
    public float FlowerDelay, DotValue;
    public bool HadDot, Projecting;
    public int Order, MirrorsLeft, MirrorTiming; // 明镜：较早防满、预留回返、贴近意气。
    public int SenCount => ((Sen & 1) != 0 ? 1 : 0) + ((Sen & 2) != 0 ? 1 : 0) + ((Sen & 4) != 0 ? 1 : 0);
}

internal sealed class Samurai100Forecast
{
    public bool Computed, CurrentOnly;
    public SamuraiPrediction Prediction;
    public uint Gcd, OffGcd;
    public int Big, Tendo, Ogi, MirrorSnow, Mirrors, Lost, Order;
    public bool OldReturn;
    public float Score, WindowScore, PotionAt = -1, EndTime, Idle, Clip, DotDelay, EarlyFlower, FlowerDelay, MirrorWaste;
    public int MirrorTiming;
    public string Trace = "";
    public int Tier => Big >= 7 && Ogi >= 2 && OldReturn ? (Tendo >= 4 ? 2 : Tendo >= 2 ? 1 : 0) : 0;
    public string Summary => CurrentOnly ? "后续时机未定，只判断当前出招" :
        $"{(Tier == 2 ? "双天道七刀" : Tier == 1 ? "常规七刀" : "当前资源输出")} 剩余预计大招={Big} 天道={Tendo} 明镜雪={MirrorSnow} 损失={Lost} " +
        $"预计续花提前={EarlyFlower:F1} 延后={DotDelay:F1} 空转={Idle:F1} 强插占时={Clip:F1} 明镜停转={MirrorWaste:F1}";
}

internal static class Samurai100Projection
{
    public const float AbilityLock = 0.65f;
    public const float PotionLock = 1.35f;
    private const float Margin = Samurai100Helper.EffectMargin;

    // 比较几种合理顺序，每次只取下一步。
    public static Samurai100Forecast Find(Samurai100State state, float window, float potionWait = -1, bool trace = false)
    {
        // 不猜玩家何时停止移动或回到距离内。
        if (state.Moving || state.Distance > state.IaiRange) return Current(state, window, potionWait >= 0, trace);
        Samurai100Forecast? best = null;
        var candidates = potionWait < 0 ? 1 : Math.Min(16, 2 + (int)(potionWait / state.Gcd));
        var seconds = ForecastSeconds(state, potionWait < 0 ? window : potionWait + 30);
        var mirrors = state.UseMirror ? Math.Min(3, (int)(state.MirrorCharges + seconds / 55)) : 0;
        if (state.MirrorRequested && !state.AutoMirror) mirrors = Math.Min(1, mirrors);
        for (var delay = 0; delay < candidates; delay++)
        for (var flower = 0; flower < 2; flower++)
        for (var timing = 0; timing < (mirrors > 0 ? 3 : 1); timing++)
        for (var order = 0; order < (mirrors + 1) * 4; order++)
        {
            if (timing == 1 && (potionWait >= 0 || state.Potion > 0)) continue;
            var input = state;
            input.FlowerDelay = flower == 0 ? 0 : state.Gcd;
            input.MirrorTiming = timing;
            input.HadDot = input.Dot > 0;
            input.DotValue = 50f / 3 * 1.13f;
            var candidate = Run(input, window, potionWait, delay * state.Gcd, order, trace);
            if (candidate.CurrentOnly) continue;
            if (potionWait >= 0 && candidate.PotionAt < 0) continue;
            if (best == null || Better(candidate, best, potionWait < 0 && Samurai100Rules.PreferOpeningMirror(state))) best = candidate;
        }
        return best ?? Current(state, window, potionWait >= 0, trace);
    }

    private static Samurai100Forecast Current(Samurai100State s, float window, bool beforePotion, bool trace)
    {
        var result = new Samurai100Forecast { Computed = true, CurrentOnly = true };
        if (s.GcdLeft >= AbilityLock)
        {
            var mirror = false;
            if (s.Moving && !beforePotion && s.UseMirror && s.MirrorCharges >= 1 && s.MirrorStacks == 0 &&
                s.Tendo <= 0 && Combo(s) == 0 && s.Distance <= s.MeleeRange)
            {
                // 开镜沿用同一份资源安排，不在移动时变成好了就开。
                var standing = s; standing.Moving = false;
                mirror = Find(standing, window).OffGcd == SAMSkill.明镜止水;
            }
            result.OffGcd = mirror ? SAMSkill.明镜止水 : ChooseOff(s, false, beforePotion);
        }
        Advance(ref s, s.GcdLeft, result);
        result.Gcd = ChooseGcd(s, 0, beforePotion, s.MirrorStacks > 0 && (s.Sen & 1) == 0);
        if (trace) result.Trace = $"当前GCD={result.Gcd} 当前穿插={result.OffGcd}";
        result.EndTime = s.Time;
        return result;
    }

    private static bool Better(Samurai100Forecast next, Samurai100Forecast old, bool openingMirror)
    {
        // 先保资源和续花，再比较整段收益。
        if ((next.Idle > .01f) != (old.Idle > .01f)) return next.Idle < old.Idle;
        if (next.Lost != old.Lost) return next.Lost < old.Lost;
        if (next.MirrorSnow != old.MirrorSnow) return next.MirrorSnow < old.MirrorSnow;
        if ((next.EarlyFlower > 5) != (old.EarlyFlower > 5)) return next.EarlyFlower < old.EarlyFlower;
        if ((next.MirrorWaste > .1f) != (old.MirrorWaste > .1f)) return next.MirrorWaste < old.MirrorWaste;
        if ((next.DotDelay > .05f) != (old.DotDelay > .05f)) return next.DotDelay < old.DotDelay;
        if (openingMirror && (next.OffGcd == SAMSkill.明镜止水) != (old.OffGcd == SAMSkill.明镜止水))
            return next.OffGcd == SAMSkill.明镜止水;
        var difference = next.Score - old.Score;
        if (Math.Abs(difference) > 1) return difference > 0;
        return next.PotionAt >= 0 && next.PotionAt < old.PotionAt;
    }

    private static Samurai100Forecast Run(Samurai100State s, float window, float wait, float notBefore, int order, bool trace)
    {
        var result = new Samurai100Forecast { Computed = true, Order = order, FlowerDelay = s.FlowerDelay, MirrorTiming = s.MirrorTiming };
        var end = wait >= 0 ? wait + 30 : window;
        // 同一段后续一起比较，不能把明镜雪藏到窗口外。
        var horizon = ForecastSeconds(s, end);
        var start = wait >= 0 ? float.PositiveInfinity : s.WindowStart;
        var oldReturn = s.ReturnIsOld && s.ReturnLeft > 0;
        var maxMirrors = Math.Max(s.MirrorRequested ? 1 : 0, order / 4);
        s.Projecting = true; s.Order = order; s.MirrorsLeft = maxMirrors;
        var recoverSnow = s.MirrorStacks > 0 && (s.Sen & 1) == 0;
        var first = true;
        var gcds = 0;
        uint other = 0;
        var predict = SAMSettings.Instance.显示技能预测 || SAMSettings.Instance.显示下G横幅;
        var initialGcd = s.GcdLeft;
        Weave(ref s, result, initialGcd, wait, notBefore, ref start, ref end, horizon, maxMirrors, true, trace);
        Advance(ref s, Math.Max(0, initialGcd - s.Time), result);
        for (var step = 0; step < 64 && s.Time + Margin < horizon; step++)
        {
            var action = ChooseGcd(s, order, wait >= 0 && s.Time < start, recoverSnow);
            // 无法垫刀时先保护资源，计入强插占用的时间。
            for (var emergency = 0; emergency < 3; emergency++)
            {
                var off = Samurai100Weave.Emergency(s, action, wait >= 0 && s.Time < start);
                if (off == 0) break;
                if (trace) result.Trace += $"{s.Time:F2}:强插{off} ";
                ApplyOff(ref s, off, result, s.Time + Margin >= start && s.Time + Margin < end);
                result.Clip += Math.Max(0, AbilityLock - s.GcdLeft);
                Advance(ref s, AbilityLock, result);
                action = ChooseGcd(s, order, wait >= 0 && s.Time < start, recoverSnow);
            }
            if (first) { result.Gcd = action; first = false; }
            if (predict && result.Prediction.Action == 0)
            {
                if (action == 0)
                    result.Prediction = SamuraiPrediction.Key(s, Samurai100Rules.WaitingIaijutsu(s, KeepSen(s)),
                        gcds + 1, waiting: gcds == 0 ? Samurai100Rules.WaitReason(s) : null);
                else result.Prediction = SamuraiPrediction.Key(s, action, gcds + 1, other);
                other = SamuraiPrediction.OtherFinisher(s, action);
            }
            if (action == 0)
            {
                var seconds = WaitSeconds(s);
                if (trace) result.Trace += $"{s.Time:F2}:等待 ";
                if (!float.IsFinite(seconds)) { result.CurrentOnly = true; break; }
                // 等待只经过时间，不产生GCD和新的穿插位。
                result.Idle += Math.Min(seconds, Math.Max(0, horizon - s.Time));
                Advance(ref s, Math.Min(seconds, Math.Max(0, horizon - s.Time)), result);
                continue;
            }
            gcds++;
            var at = s.Time;
            var cast = action == SAMSkill.彼岸花 || action == SAMSkill.纷乱雪月花 ? s.Cast : action == SAMSkill.奥义斩浪 ? s.OgiCast : 0;
            var hit = at + cast + Margin;
            var inWindow = hit >= start && hit < end;
            if (trace) result.Trace += $"{at:F2}:{action}({s.Sen}/{s.MirrorStacks}) ";
            ApplyGcd(ref s, action, result, inWindow, oldReturn, hit);
            if (action == SAMSkill.纷乱雪月花) oldReturn = hit < start;
            if (action == SAMSkill.燕回返) oldReturn = false;
            var next = at + s.Gcd;
            Advance(ref s, Math.Min(s.Gcd, cast > 0 ? cast + Margin : AbilityLock), result);
            Weave(ref s, result, next, wait, notBefore, ref start, ref end, horizon, maxMirrors, false, trace);
            Advance(ref s, Math.Max(0, next - s.Time), result);
        }
        result.EndTime = s.Time;
        result.Computed = !first;
        // 段尾资源也计价，避免为了赶在推算结束前出手而乱花资源。
        result.Score += s.SenCount * 300 + s.MirrorCharges * 1300 + s.MirrorStacks * 180 +
            s.Kenki * 10 + s.Meditation * 200 + (Combo(s) == SAMSkill.晓风 ? 60 : Combo(s) != 0 ? 150 : 0);
        if (s.ReturnLeft > 0) result.Score += (s.ReturnTendo ? 1100 : 680) * 1.5f;
        if (s.Tendo > 0) result.Score += (1100 - 680) * 3;
        if (s.OgiReturn) result.Score += 1500;
        if (s.UseOgi && s.Ogi > 0) result.Score += 3000;
        // 只计算本段实际覆盖，不能重复奖励每次刷新后的整分钟。
        if (s.UseDot) result.Score -= Math.Max(0, s.Dot) * s.DotValue;
        result.Score -= result.MirrorWaste * 1300 / 55;
        return result;
    }

    // 短药窗结束后也要看到下一次续花，不能把损失推到段尾之外。
    private static float ForecastSeconds(Samurai100State s, float end) =>
        Math.Max(end + 20, s.UseDot ? Math.Clamp(s.Dot + 3 * s.Gcd, 20, 70) : 0);

    private static void Weave(ref Samurai100State s, Samurai100Forecast r, float until, float wait, float notBefore,
        ref float start, ref float end, float horizon, int maxMirrors, bool current, bool trace)
    {
        for (var slot = 0; slot < Math.Clamp(s.MaxWeaves, 1, 2) && s.Time + AbilityLock <= until + .001f; slot++)
        {
            if (wait >= 0 && r.PotionAt < 0 && s.Time >= Math.Max(notBefore, s.PotionReadyAt) && s.Time <= wait &&
                slot == 0 && s.Time + PotionLock <= until)
            {
                r.PotionAt = s.Time;
                start = s.Time; end = start + 30;
                s.Potion = 30;
                if (trace) r.Trace += $"{s.Time:F2}:药 ";
                Advance(ref s, PotionLock, r);
                break;
            }
            var preparingPotion = wait >= 0 && s.Time < start;
            var mirrorFits = s.Time + (Math.Max(0, 3 - s.SenCount) + 1) * s.Gcd + s.Cast < horizon;
            var action = ChooseOff(s, r.Mirrors < maxMirrors && mirrorFits, preparingPotion);
            if (current && slot == 0) r.OffGcd = action;
            if (action == 0 && !preparingPotion)
            {
                var delay = Samurai100Weave.CooldownDelay(s);
                if (float.IsFinite(delay) && s.Time + delay + AbilityLock <= until)
                {
                    Advance(ref s, delay, r);
                    action = ChooseOff(s, r.Mirrors < maxMirrors && mirrorFits, false);
                }
            }
            if (action == 0) break;
            if (trace) r.Trace += $"{s.Time:F2}:能力{action} ";
            ApplyOff(ref s, action, r, s.Time + Margin >= start && s.Time + Margin < end);
            Advance(ref s, AbilityLock, r);
        }
        if (wait >= 0 && s.Time < start) return;
        var tailDelay = Samurai100Weave.ReadyIn(s);
        var left = until - s.Time;
        if (tailDelay < 0 || tailDelay >= left || left - tailDelay >= AbilityLock) return;
        Advance(ref s, tailDelay, r);
        var tailAction = Samurai100Weave.Emergency(s, 0, false);
        if (tailAction == 0) return;
        if (trace) r.Trace += $"{s.Time:F2}:末尾强插{tailAction} ";
        ApplyOff(ref s, tailAction, r, s.Time + Margin >= start && s.Time + Margin < end);
        r.Clip += Math.Max(0, AbilityLock - s.GcdLeft);
        Advance(ref s, AbilityLock, r);
    }

    private static uint ChooseGcd(Samurai100State s, int order, bool beforePotion, bool recoverSnow)
    {
        var action = ChooseBaseGcd(s, order, beforePotion, recoverSnow);
        return Samurai100Weave.BeforeCast(s, action, beforePotion);
    }

    private static uint ChooseBaseGcd(Samurai100State s, int order, bool beforePotion, bool recoverSnow)
    {
        var cast = s.Cast + Margin;
        var canCast = !s.Moving && s.Distance <= s.IaiRange;
        var controlled = Samurai100Rules.TryTsubameControl(s, out var useReturn, out _);
        var hasReturn = s.ReturnLeft > Margin && s.Distance <= s.IaiRange;
        var canReturn = hasReturn && (!controlled || useReturn);
        if (s.OgiReturn && s.Distance <= s.OgiRange) return SAMSkill.回返斩浪;
        if (controlled && canReturn) return SAMSkill.燕回返;
        if (canReturn && s.Immediate) return SAMSkill.燕回返;
        if (Samurai100Rules.HiganbanaDue(s) && s.Moon > cast && canCast)
            return SAMSkill.彼岸花;
        if (Samurai100Rules.PaddingGcd(s, out var filler)) return filler;
        if (canReturn && s.ReturnLeft <= 2 * s.Gcd + Margin) return SAMSkill.燕回返;
        // 三闪已有旧回返，移动时也先清掉，不能误等雪月花。
        if (canReturn && s.SenCount == 3 && !canCast) return SAMSkill.燕回返;
        var ogi = s.UseOgi && s.Ogi > s.OgiCast + Margin && !s.Moving && s.Distance <= s.OgiRange && s.Moon > s.OgiCast;
        if (s.DelayReturn && !s.ForcePadding && hasReturn && !canReturn && s.ReturnLeft <= 2 * s.Gcd + Margin) ogi = false;
        var flowerSafe = !s.UseDot || s.Dot + s.FlowerDelay > (ToFlower(s) + 2) * s.Gcd + cast;
        var returnSafe = Samurai100Rules.ReturnBeforeFlower(s);
        var holdReturn = canReturn && canCast && !beforePotion && s.Potion <= 0 && s.Party <= 0 && !s.Dump &&
            !s.Immediate && (order & 2) == 0 && s.UseIki &&
            !Samurai100Rules.Spending(s) && s.IkiCd <= 6 * s.Gcd && s.ReturnLeft > s.IkiCd + s.Gcd + Margin;
        if (ogi && flowerSafe && s.Ogi <= 2 * s.Gcd + s.OgiCast) return SAMSkill.奥义斩浪;
        if (s.SenCount == 3 && canCast && s.Moon > cast)
        {
            // 为待吃药腾出一个瞬发插入位，最多垫前两刀。
            if (beforePotion && !s.Immediate && s.MirrorStacks == 0 && (order & 2) != 0 && s.Distance <= s.MeleeRange)
            {
                if (s.Combo == 0) return SAMSkill.晓风;
                if (s.Combo == SAMSkill.晓风) return s.Moon <= s.Flower ? SAMSkill.阵风 : SAMSkill.士风;
            }
            // 用后续仍能收尾的前两刀靠近意气，不停手等爆发。
            if (holdReturn && s.MirrorStacks == 0 && s.Distance <= s.MeleeRange && flowerSafe &&
                s.IkiCd <= (Combo(s) == 0 ? 3 : 2) * s.Gcd && (s.Tendo <= 0 || s.Tendo > s.IkiCd + 2 * s.Gcd + cast))
            {
                if (Combo(s) == 0) return SAMSkill.晓风;
                if (Combo(s) == SAMSkill.晓风) return s.Moon <= s.Flower ? SAMSkill.阵风 : SAMSkill.士风;
            }
            // 禁用使用不等于没有回返，不能直接用新居合覆盖。
            if (hasReturn) return canReturn ? SAMSkill.燕回返 : 0;
            if (ogi && flowerSafe && (order & 1) != 0 && s.Tendo <= 0) return SAMSkill.奥义斩浪;
            return SAMSkill.纷乱雪月花;
        }
        var requested = RequestedPreparation(s);
        if (requested != 0) return requested;
        var mirrorSafe = s.MirrorStacks == 0 || s.MirrorLeft > (s.MirrorStacks + 2) * s.Gcd + Margin;
        if (canReturn && !beforePotion && returnSafe && mirrorSafe &&
            (Samurai100Rules.Spending(s) || Samurai100Rules.FinishingReturn(s) || s.Potion > Margin || s.Party > Margin || s.Dump)) return SAMSkill.燕回返;
        // 也比较先取雪开镜，再打奥义的顺序。
        var prepareMirror = s.Projecting && s.MirrorsLeft > 0 && s.UseMirror && s.MirrorStacks == 0 &&
            s.MirrorCharges >= 1 && s.Ogi > 5 * s.Gcd + s.OgiCast && s.Distance <= s.MeleeRange;
        if (ogi && flowerSafe && mirrorSafe && ((order & 1) != 0 || s.Tendo <= 0 && !prepareMirror)) return SAMSkill.奥义斩浪;
        if (canReturn && !beforePotion && returnSafe && ((order & 2) != 0 || s.Distance > s.MeleeRange)) return SAMSkill.燕回返;
        if (canReturn && !beforePotion && returnSafe && s.MirrorStacks == 0 && KeepSen(s) &&
            (Combo(s) == SAMSkill.阵风 || Combo(s) == SAMSkill.士风) &&
            (!holdReturn || s.Dot > cast + 5)) return SAMSkill.燕回返;
        var waiting = Samurai100Rules.WaitingIaijutsu(s, KeepSen(s));
        if (waiting != 0 && !canCast) return 0;
        if (s.Distance > s.MeleeRange) return canReturn ? SAMSkill.燕回返 : 0;
        if (s.MirrorStacks > 0)
        {
            if (s.SenCount == 3) return canReturn ? SAMSkill.燕回返 : 0;
            if (s.SenCount == 1 && KeepSen(s))
            {
                if (canReturn && s.Dot > cast + s.Gcd) return SAMSkill.燕回返;
                return canCast && s.Moon > cast && Samurai100Rules.CanRefreshHiganbana(s) ? SAMSkill.彼岸花 : 0;
            }
            var finisher = Samurai100Rules.ChooseMoonFlower(s);
            if (finisher != 0) return finisher;
            return (recoverSnow || s.Dump) && (s.Sen & 1) == 0 ? SAMSkill.雪风 : 0;
        }
        var combo = Combo(s);
        var keep = KeepSen(s);
        if (combo == SAMSkill.阵风 || combo == SAMSkill.士风)
        {
            if ((s.SenCount == 3 || keep) && s.Moon <= cast) return SAMSkill.晓风;
            if (s.SenCount == 3) return canReturn ? SAMSkill.燕回返 : 0;
            if (keep) return canCast && s.Moon > cast && Samurai100Rules.CanRefreshHiganbana(s) ? SAMSkill.彼岸花 : 0;
            var bit = combo == SAMSkill.阵风 ? 2 : 4;
            return (s.Sen & bit) == 0 ? bit == 2 ? SAMSkill.月光 : SAMSkill.花车 : SAMSkill.晓风;
        }
        if (combo != SAMSkill.晓风) return SAMSkill.晓风;
        if (s.Moon <= 0 && s.Flower <= 0 && s.SenCount == 0) return SAMSkill.士风;
        if (s.Moon <= cast + 3 * s.Gcd && (s.Moon <= s.Flower || keep || s.SenCount == 3)) return SAMSkill.阵风;
        if (s.Flower <= cast + 3 * s.Gcd) return SAMSkill.士风;
        if (keep || s.SenCount == 3) return s.Moon <= s.Flower ? SAMSkill.阵风 : SAMSkill.士风;
        if (LongSenFitsFlower(s)) return Samurai100Rules.ChooseMoonFlower(s) == SAMSkill.月光 ? SAMSkill.阵风 : SAMSkill.士风;
        if ((s.Sen & 1) == 0) return SAMSkill.雪风;
        return Samurai100Rules.ChooseMoonFlower(s) == SAMSkill.月光 ? SAMSkill.阵风 : SAMSkill.士风;
    }

    private static uint Combo(Samurai100State s) => s.ComboLeft > Margin ? s.Combo : 0;

    // 雪连取闪后垫满仍太早时，多一刀长连避免后面空等或重新攒三闪。
    internal static bool LongSenFitsFlower(Samurai100State s)
    {
        if (!s.UseDot || s.SenCount != 0 || Combo(s) != SAMSkill.晓风 || s.MirrorStacks > 0 || s.Dot <= 0) return false;
        if (s.UseMirror && s.MirrorCharges + s.Gcd / 55 >= 1 && s.Tendo <= 0) return false;
        var fillers = 2 + (s.ReturnLeft > 4 * s.Gcd + Margin && !Samurai100Rules.HoldTsubame(s) ? 1 : 0);
        var early = s.Dot - ((1 + fillers) * s.Gcd + s.Cast + Margin);
        return early > 5 && early <= 5 + s.Gcd + Margin;
    }

    internal static int ToThree(Samurai100State s)
    {
        var snow = (s.Sen & 1) != 0; var moon = (s.Sen & 2) != 0; var flower = (s.Sen & 4) != 0;
        var count = (snow ? 0 : 2) + (moon ? 0 : 3) + (flower ? 0 : 3);
        var stacks = s.MirrorStacks;
        if (!moon && stacks > 0) { count -= 2; stacks--; }
        if (!flower && stacks > 0) count -= 2;
        if (s.MirrorStacks == 0)
        {
            var combo = Combo(s);
            if (combo == SAMSkill.晓风 && count > 0) count--;
            if (combo == SAMSkill.阵风 && !moon || combo == SAMSkill.士风 && !flower) count -= 2;
        }
        return count;
    }

    internal static int ToFlower(Samurai100State s)
    {
        if (s.SenCount == 1) return 0;
        if (s.SenCount == 0) return s.MirrorStacks > 0 || Combo(s) != 0 ? 1 : 2;
        return FlowerAfterThree(s);
    }

    private static int FlowerAfterThree(Samurai100State s)
    {
        var returns = (s.ReturnLeft > 0 ? 1 : 0) + (s.Immediate ? 1 : 0);
        var count = ToThree(s) + 1 + (s.MirrorStacks > 3 - s.SenCount ? 1 : 2) + returns;
        if (!s.UseMirror || s.MirrorStacks > 0 || s.Tendo > 0 || s.Projecting && s.MirrorsLeft <= 0) return count;
        // 手搓途中转好的明镜也能缩短取闪，不能过早把一闪锁死。
        var next = s;
        next.UseDot = next.UseOgi = false;
        next.OgiReturn = false; next.ReturnLeft = 0;
        var check = new Samurai100Forecast();
        Advance(ref next, next.GcdLeft, check);
        for (var before = 0; before <= 8; before++)
        {
            if ((next.Sen & 1) != 0 && Combo(next) == 0 && next.MirrorCharges >= 1 && MirrorTimeReady(next))
                count = Math.Min(count, before + (3 - next.SenCount) + 2 + returns);
            if (next.SenCount == 3) break;
            var action = ChooseGcd(next, 0, false, false);
            if (action == 0) break;
            ApplyGcd(ref next, action, check, false, false, next.Time + Margin);
            Advance(ref next, next.Gcd, check);
        }
        return count;
    }

    internal static bool KeepSen(Samurai100State s)
    {
        if (!s.UseDot || s.SenCount != 1) return false;
        var fillers = s.MirrorStacks > 0 ? 0 : Combo(s) == 0 ? 2 : Combo(s) == SAMSkill.晓风 ? 1 : 0;
        if (s.ReturnLeft > s.GcdLeft + Margin && !Samurai100Rules.HoldTsubame(s)) fillers++;
        // 垫刀不足只留一G恢复余量；移动仍保留已经垫好的连击。
        if (!s.Moving && s.Dot > s.GcdLeft + (fillers + 1) * s.Gcd + s.Cast + Margin + 5) return false;
        return s.Dot + s.FlowerDelay <= s.GcdLeft + FlowerAfterThree(s) * s.Gcd + s.Cast + Margin;
    }

    internal static float WaitSeconds(Samurai100State s)
    {
        if (!s.Moving && s.Distance <= s.IaiRange &&
            Samurai100Rules.WaitingIaijutsu(s, KeepSen(s)) == SAMSkill.彼岸花)
        {
            var seconds = s.Dot - (s.Cast + Margin + 5);
            if (seconds > 0 && seconds <= s.Gcd + Margin) return seconds + .001f;
        }
        return float.PositiveInfinity;
    }

    // 只为明确请求准备雪连，不锁定后续序列。
    internal static uint RequestedPreparation(Samurai100State s)
    {
        if (!s.MirrorRequested || !s.UseMirror || s.MirrorStacks > 0 || s.Tendo > 0 ||
            Combo(s) != SAMSkill.晓风 || (s.Sen & 1) != 0 || s.Distance > s.MeleeRange) return 0;
        var weaveAt = Math.Max(AbilityLock, (1 - s.MirrorCharges) * 55);
        if (weaveAt + AbilityLock + Margin > s.Gcd) return 0;
        if (s.Moon > 0 && s.Moon <= weaveAt + Margin || s.Flower > 0 && s.Flower <= weaveAt + Margin) return 0;
        var check = new Samurai100Forecast();
        ApplyGcd(ref s, SAMSkill.雪风, check, false, false, s.Time + Margin);
        Advance(ref s, weaveAt, check);
        return CanMirror(s, out _, true) ? SAMSkill.雪风 : 0;
    }

    internal static bool CanMirror(Samurai100State s) => CanMirror(s, out _);

    internal static float RequestedMirrorDelay(Samurai100State s, bool allowClip = false)
    {
        if (!s.MirrorRequested || s.MirrorCharges >= 1) return float.PositiveInfinity;
        var delay = (1 - s.MirrorCharges) * 55;
        if (delay + (allowClip ? 0 : AbilityLock) > s.GcdLeft) return float.PositiveInfinity;
        Advance(ref s, delay, new Samurai100Forecast());
        s.MirrorCharges = Math.Max(1, s.MirrorCharges);
        return CanMirror(s) ? delay : float.PositiveInfinity;
    }

    internal static bool CanMirror(Samurai100State s, out string reason, bool preserveBuffs = false)
    {
        reason = "三层明镜无法用月花正常衔接，先调整资源";
        if (!s.UseMirror) { reason = "未开启明镜QT，也没有一次请求"; return false; }
        if (s.MirrorCharges < 1) { reason = "明镜请求等待充能"; return false; }
        if (s.MirrorStacks > 0) { reason = "先用完已有明镜"; return false; }
        if (s.Tendo > 0) { reason = "先兑现已有天道"; return false; }
        if (Combo(s) != 0) { reason = "先收原连击，再执行明镜请求"; return false; }
        if (s.MirrorRequested)
        {
            s.Dump = false;
            s.Distance = 0;
            // 无目标时，不能把未知彼岸花当成需要立刻续花。
            if (s.TargetUnavailable) s.UseDot = false;
        }
        else if (s.Distance > s.MeleeRange || s.TargetUnavailable) return false;
        s.MirrorRequested = s.MirrorForced = false;
        // 检查资源能否衔接，不把当前移动当成后续一直不能读条。
        s.Moving = false;
        if (KeepSen(s)) { reason = "先用已有一闪续花"; return false; }
        if (!s.Dump && (s.Sen & 1) == 0 && !(s.SenCount == 0 && s.UseDot && s.Dot <= 2 * s.Gcd + s.Cast)) return false;
        s.MirrorStacks = 3; s.MirrorLeft = 20; s.Tendo = 30;
        var check = new Samurai100Forecast();
        var moon = s.Moon > 0; var flower = s.Flower > 0;
        Advance(ref s, s.GcdLeft, check);
        // 三层之后接好居合；近期要续花时也检查，不能留下死角。
        var needsFlower = s.UseDot && s.Dot <= 30;
        s.UseMirror = false;
        for (var step = 0; step < 40; step++)
        {
            if (preserveBuffs && (moon && s.Moon <= Margin || flower && s.Flower <= Margin)) return false;
            var action = ChooseGcd(s, 0, false, false);
            if (action == 0 || action == SAMSkill.雪风 && s.MirrorStacks > 0 && !s.Dump) return false;
            if (action == SAMSkill.彼岸花)
            {
                if (s.Dot - s.Cast - Margin > 5 || s.Cast + Margin - s.Dot > s.Gcd) return false;
                needsFlower = false;
            }
            ApplyGcd(ref s, action, check, false, false, s.Time + s.Cast + Margin);
            if (s.MirrorStacks == 0 && !needsFlower &&
                (action == SAMSkill.彼岸花 || action == SAMSkill.纷乱雪月花))
            { reason = "资源可衔接，执行一次明镜请求"; return true; }
            if (s.MirrorStacks > 0 && s.MirrorLeft <= s.Gcd + Margin) return false;
            Advance(ref s, s.Gcd, check);
        }
        return false;
    }

    internal static uint NextResourceGcd(Samurai100State s)
    {
        var check = new Samurai100Forecast();
        Advance(ref s, s.GcdLeft, check);
        return ChooseGcd(s, s.Projecting ? s.Order : 0, false, true);
    }

    internal static uint NextBaseGcd(Samurai100State s)
    {
        s = AtNextGcd(s);
        return ChooseBaseGcd(s, s.Projecting ? s.Order : 0, false, true);
    }

    internal static Samurai100State AtNextGcd(Samurai100State s)
    {
        Advance(ref s, s.GcdLeft, new Samurai100Forecast());
        return s;
    }

    // 再留到下一处穿插，会不会让下一发照破转不回来。
    internal static bool ShohaCooldownRisk(Samurai100State s)
    {
        if (s.Moving || s.Distance > s.IaiRange) return false;
        var first = NextResourceGcd(s);
        if (first == 0) return false;
        var check = new Samurai100Forecast();
        var next = s.Time + s.GcdLeft;
        var deadline = next + 15 + 2 * AbilityLock;
        var gains = 0;
        // 后续只统计产压，不能递归选择照破。
        s.UseShoha = false; s.Meditation = 0;
        for (var step = 0; step < 20; step++)
        {
            Advance(ref s, Math.Max(0, next - s.Time), check);
            if (s.Time >= deadline) return false;
            var action = step == 0 ? first : ChooseGcd(s, s.Projecting ? s.Order : 0, false, false);
            if (action == 0) return false;
            var cast = Samurai100Weave.CastTime(s, action);
            if (cast > 0 && ++gains == 4) return true;
            ApplyGcd(ref s, action, check, false, false, s.Time + cast + Margin);
            next = s.Time + s.Gcd;
            Advance(ref s, cast > 0 ? cast + Margin : AbilityLock, check);
            if (step == 0) deadline = s.Time + 15 + AbilityLock;
            for (var slot = 0; slot < Math.Clamp(s.MaxWeaves, 1, 2) && s.Time + AbilityLock <= next; slot++)
            {
                var off = ChooseOff(s, false, false, true);
                if (off == 0) break;
                ApplyOff(ref s, off, check, false);
                Advance(ref s, AbilityLock, check);
            }
        }
        return false;
    }

    internal static int BeforeIkishotenLimit(Samurai100State s)
    {
        var income = 0;
        var check = new Samurai100Forecast();
        var next = s.Time + s.GcdLeft;
        var maxWeaves = Math.Clamp(s.MaxWeaves, 1, 2);
        // 看到真正能插入意气的位置，不把CD归零当成已经能用。
        for (var step = 0; step < 12; step++)
        {
            var slots = maxWeaves;
            if (s.UseShoha && s.Meditation == 3 && s.Distance <= s.ShohaRange &&
                s.Time + s.ShohaCd + AbilityLock <= next)
            {
                var ready = s;
                Advance(ref ready, s.ShohaCd, check);
                if (Samurai100Weave.ShouldShoha(ready))
                {
                    s = ready;
                    ApplyOff(ref s, SAMSkill.照破, check, false);
                    Advance(ref s, AbilityLock, check);
                    slots--;
                }
            }
            if (slots > 0 && s.Time + s.IkiCd + AbilityLock <= next)
            {
                Advance(ref s, s.IkiCd, check);
                s.MaxWeaves = slots;
                // 预留检查与执行余量，不赌转好后一帧不差的极限双插。
                var spendAfter = Samurai100Rules.CanSpendAfterIkishoten(s) && s.GcdLeft >= 2 * AbilityLock + Margin;
                var after = spendAfter ? 0 : Samurai100Rules.NextGain(s);
                return 50 - income - after;
            }
            Advance(ref s, Math.Max(0, next - s.Time), check);
            var action = ChooseGcd(s, s.Projecting ? s.Order : 0, false, true);
            if (action == 0) break;
            income += Samurai100Rules.KenkiGain(action);
            ApplyGcd(ref s, action, check, false, false, s.Time + Margin);
            next = s.Time + s.Gcd;
            var cast = action == SAMSkill.彼岸花 || action == SAMSkill.纷乱雪月花 ? s.Cast : action == SAMSkill.奥义斩浪 ? s.OgiCast : 0;
            Advance(ref s, cast > 0 ? cast + Margin : AbilityLock, check);
        }
        return 50 - income;
    }

    internal static SamuraiPrediction Preview(Samurai100State s, uint first)
    {
        var check = new Samurai100Forecast();
        Advance(ref s, s.GcdLeft, check);
        uint other = 0;
        // 无爆发方案时也复用同一份选招和资源推进。
        for (var gcds = 1; gcds <= 8; gcds++)
        {
            var action = gcds == 1 ? first : ChooseGcd(s, 0, false, s.MirrorStacks > 0 && (s.Sen & 1) == 0);
            var prediction = SamuraiPrediction.Key(s, action, gcds, other);
            if (prediction.Action != 0) return prediction;
            if (action == 0)
                return SamuraiPrediction.Key(s, Samurai100Rules.WaitingIaijutsu(s, KeepSen(s)), gcds,
                    waiting: gcds == 1 ? Samurai100Rules.WaitReason(s) : null);
            other = SamuraiPrediction.OtherFinisher(s, action);
            ApplyGcd(ref s, action, check, false, false, s.Time + s.Cast + Margin);
            Advance(ref s, s.Gcd, check);
        }
        return default;
    }

    internal static int IncomeBefore(Samurai100State s, float seconds, bool minimum = true)
    {
        // 候选内部按同一条路预算；基础兜底才取保守边界。
        if (s.Projecting) return ProjectIncome(s, seconds, s.Order);
        var income = ProjectIncome(s, seconds, 0);
        for (var order = 1; order < 4; order++)
        {
            var next = ProjectIncome(s, seconds, order);
            income = minimum ? Math.Min(income, next) : Math.Max(income, next);
        }
        return income;
    }

    private static int ProjectIncome(Samurai100State s, float seconds, int order)
    {
        var income = 0;
        var check = new Samurai100Forecast();
        var end = s.Time + seconds;
        var next = s.Time + s.GcdLeft;
        // 连途中的意气一起推进，奥义两刀不产剑气。
        for (var step = 0; step < 20 && s.Time < end; step++)
        {
            for (var slot = 0; slot < Math.Clamp(s.MaxWeaves, 1, 2) &&
                s.Time + AbilityLock <= next && s.Time < end; slot++)
            {
                var off = ChooseOff(s, false, false, true);
                if (off == 0) break;
                ApplyOff(ref s, off, check, false);
                Advance(ref s, AbilityLock, check);
            }
            Advance(ref s, Math.Max(0, next - s.Time), check);
            if (s.Time >= end) break;
            var action = ChooseGcd(s, order, false, true);
            if (action == 0) break;
            income += Samurai100Rules.KenkiGain(action);
            ApplyGcd(ref s, action, check, false, false, s.Time + s.Cast + Margin);
            next = s.Time + s.Gcd;
            var cast = action == SAMSkill.彼岸花 || action == SAMSkill.纷乱雪月花 ? s.Cast : action == SAMSkill.奥义斩浪 ? s.OgiCast : 0;
            Advance(ref s, cast > 0 ? cast + Margin : AbilityLock, check);
        }
        return income;
    }

    private static uint ChooseOff(Samurai100State s, bool mirror, bool beforePotion, bool budget = false)
    {
        if (Samurai100Weave.ShohaUrgent(s)) return SAMSkill.照破;
        if (!beforePotion && Samurai100Rules.CanIkishoten(s, out _)) return SAMSkill.意气冲天;
        if (!beforePotion && Samurai100Weave.SeneiReady(s)) return SAMSkill.必杀剑_闪影;
        if (!beforePotion && float.IsFinite(Samurai100Weave.CooldownDelay(s))) return 0;
        if (Samurai100Weave.ShouldShoha(s)) return SAMSkill.照破;
        if (Samurai100Rules.CanZanshin(s, budget) && (!beforePotion || s.Zanshin < 2 * s.Gcd)) return SAMSkill.残心;
        if (budget)
        {
            if (s.UseShinten && s.ShintenCd <= 0 && s.UseIki && s.IkiCd <= 0 && s.Kenki > 50 &&
                s.Distance <= s.MeleeRange) return SAMSkill.必杀剑_震天;
            return (!s.Projecting || s.MirrorsLeft > 0) && MirrorTimeReady(s) && CanMirror(s) ? SAMSkill.明镜止水 : 0;
        }
        if (!beforePotion && Samurai100Rules.Preparing(s) && s.Kenki > Samurai100Rules.BeforeIkiLimit(s) &&
            Samurai100Rules.SpendKenki(s, out _)) return SAMSkill.必杀剑_震天;
        if (s.MirrorRequested && CanMirror(s)) return SAMSkill.明镜止水;
        if (mirror && MirrorTimeReady(s) && CanMirror(s)) return SAMSkill.明镜止水;
        if (Samurai100Rules.SpendKenki(s, out _))
            return SAMSkill.必杀剑_震天;
        return 0;
    }

    private static bool MirrorTimeReady(Samurai100State s)
    {
        if (s.MirrorRequested) return true;
        if (s.Dump || s.Potion > 0 || s.Party > 0 || Samurai100Rules.Spending(s)) return true;
        if (Samurai100Rules.PreferOpeningMirror(s)) return true;
        if (s.Moon <= 3 * s.Gcd || s.Flower <= 3 * s.Gcd) return true;
        var withoutMirror = s; withoutMirror.UseMirror = false;
        if (s.UseDot && s.SenCount != 1 && s.Dot + s.FlowerDelay < ToFlower(withoutMirror) * s.Gcd + s.Cast + Margin) return true;
        // 提前覆盖一轮取闪和续花，不能等满层才开始收连击。
        if ((2 - s.MirrorCharges) * 55 <= (s.MirrorTiming == 0 ? 10 : 4) * s.Gcd) return true;
        var setup = (Math.Max(0, 3 - s.SenCount) + 1 + (s.ReturnLeft > 0 ? 1 : 0)) * s.Gcd + s.Cast;
        // 这层用完来不及再转好时，留到天道能接近120的位置。
        if (s.UseIki && s.IkiCd <= 30 && (2 - s.MirrorCharges) * 55 > s.IkiCd + 2 * s.Gcd &&
            setup < s.IkiCd - 2 * s.Gcd) return false;
        return s.UseIki && s.IkiCd <= setup + (s.MirrorTiming == 0 ? 6 : s.MirrorTiming == 1 ? 12 : 0) * s.Gcd;
    }

    private static void Damage(ref Samurai100State s, Samurai100Forecast r, float potency, bool window, bool critical = false, float delay = Margin)
    {
        // 用一致的威力权重比较顺序，不宣称实际伤害百分比。
        var value = potency * (critical ? 1.5f : 1) * (s.Moon > delay ? 1.13f : 1);
        r.Score += value;
        if (window)
        {
            r.WindowScore += value;
            // 集中资源的比较权重；没有药效时不按药内评分。
            r.Score += value * (s.Potion > delay ? 1 : Samurai100Rules.SelfWindow(s) > delay ? .1f : 0);
        }
        if (s.Party > delay) r.Score += value * .1f;
    }

    private static void ApplyGcd(ref Samurai100State s, uint action, Samurai100Forecast r, bool window, bool oldReturn, float hit)
    {
        if (action == 0) return;
        s.GcdLeft = s.Gcd;
        var sen = 0; var gain = 0; var potency = 0f; var meditation = false;
        var oldMoon = s.Moon;
        if (action == SAMSkill.晓风) { gain = 5; potency = 240; s.Combo = action; s.ComboLeft = 30; }
        else if (action == SAMSkill.阵风 || action == SAMSkill.士风)
        {
            gain = 5; potency = 300; s.Combo = action; s.ComboLeft = 30;
            if (action == SAMSkill.阵风) s.Moon = 40; else s.Flower = 40;
        }
        else if (action == SAMSkill.雪风 || action == SAMSkill.月光 || action == SAMSkill.花车)
        {
            sen = action == SAMSkill.雪风 ? 1 : action == SAMSkill.月光 ? 2 : 4;
            gain = sen == 1 ? 15 : 10; potency = sen == 1 ? 340 : 420;
            if (sen != 1 && s.Time < s.Gcd && s.NeedsPosition && s.TrueNorth <= Margin &&
                s.Position != SamuraiHelper.GetNeedPositional(action)) potency -= 50;
            if ((s.Sen & sen) != 0) r.Lost++;
            s.Sen |= sen; s.Combo = 0; s.ComboLeft = 0;
            if (s.MirrorStacks > 0 && sen != 1) { if (sen == 2) s.Moon = 40; else s.Flower = 40; }
            if (s.MirrorStacks > 0 && sen == 1) r.MirrorSnow++;
        }
        else if (action == SAMSkill.彼岸花)
        {
            var delay = s.Cast + Margin;
            var late = s.HadDot ? Math.Max(0, delay - s.Dot) : 0;
            r.DotDelay = Math.Max(r.DotDelay, late);
            r.EarlyFlower = Math.Max(r.EarlyFlower, s.Dot - delay);
            if (late > s.Gcd + .001f) r.Lost++;
            r.Score -= Math.Max(0, s.Dot - delay) * s.DotValue;
            s.DotValue = 50f / 3 * (s.Moon > delay ? 1.13f : 1) *
                (1 + (s.Potion > delay ? 1 : 0) + (s.Party > delay ? .1f : 0));
            r.Score += 60 * s.DotValue;
            potency = 200;
            s.Dot = 60 + delay; s.HadDot = true; s.Sen = 0; meditation = true;
        }
        else if (action == SAMSkill.纷乱雪月花)
        {
            var tendo = s.Tendo > s.Cast + Margin;
            Damage(ref s, r, tendo ? 1100 : 680, window, true, s.Cast + Margin);
            if (s.ReturnLeft > 0) r.Lost++;
            s.Sen = 0; s.Tendo = 0; s.ReturnLeft = 30 + s.Cast; s.ReturnTendo = tendo; meditation = true;
            if (window) { r.Big++; if (tendo) r.Tendo++; }
        }
        else if (action == SAMSkill.燕回返)
        {
            Damage(ref s, r, s.ReturnTendo ? 1100 : 680, window, true);
            if (window) { r.Big++; if (s.ReturnTendo) r.Tendo++; if (oldReturn) r.OldReturn = true; }
            s.ReturnLeft = 0;
        }
        else if (action == SAMSkill.奥义斩浪 || action == SAMSkill.回返斩浪)
        {
            Damage(ref s, r, 1000, window, true, action == SAMSkill.奥义斩浪 ? s.OgiCast + Margin : Margin);
            if (window) { r.Big++; r.Ogi++; }
            s.OgiReturn = action == SAMSkill.奥义斩浪; s.Ogi = 0; meditation = s.OgiReturn;
        }
        if (potency > 0)
        {
            var newMoon = s.Moon; s.Moon = oldMoon;
            Damage(ref s, r, potency, window, false, action == SAMSkill.彼岸花 ? s.Cast + Margin : Margin);
            s.Moon = newMoon;
        }
        if (gain > 0)
        {
            if (s.Kenki + gain > 100 && s.UseShinten) r.Lost++;
            s.Kenki = Math.Min(100, s.Kenki + gain);
            if (s.MirrorStacks > 0) { s.MirrorStacks--; if (s.MirrorStacks == 0) s.MirrorLeft = 0; }
        }
        if (meditation)
        {
            if (s.Meditation == 3 && s.UseShoha) r.Lost++;
            s.Meditation = Math.Min(3, s.Meditation + 1);
        }
    }

    private static void ApplyOff(ref Samurai100State s, uint action, Samurai100Forecast r, bool window)
    {
        if (action == SAMSkill.明镜止水)
        {
            s.MirrorCharges--; s.MirrorStacks = 3; s.MirrorLeft = 20; s.Tendo = 30; r.Mirrors++; s.MirrorsLeft--;
            if (s.MirrorRequested) { s.MirrorRequested = s.MirrorForced = false; s.UseMirror = s.AutoMirror; }
        }
        if (action == SAMSkill.意气冲天)
        { s.Kenki = Math.Min(100, s.Kenki + 50); s.Ogi = 30; s.Zanshin = 30; s.IkiCd = 120; }
        if (action == SAMSkill.照破)
        { Damage(ref s, r, 640, window); s.Meditation = 0; s.ShohaCd = 15; }
        if (action == SAMSkill.残心)
        { Damage(ref s, r, 940, window); s.Kenki -= 50; s.Zanshin = 0; s.ZanshinCd = 1; }
        if (action == SAMSkill.必杀剑_闪影)
        { Damage(ref s, r, 800, window); s.Kenki -= 25; s.SeneiCd = 60; }
        if (action == SAMSkill.必杀剑_震天)
        { Damage(ref s, r, 250, window); s.Kenki -= 25; s.ShintenCd = 1; }
    }

    private static void Advance(ref Samurai100State s, float time, Samurai100Forecast r)
    {
        if (time <= 0) return;
        if (s.MirrorLeft > 0 && s.MirrorLeft <= time && s.MirrorStacks > 0) r.Lost += s.MirrorStacks;
        if (s.ReturnLeft > 0 && s.ReturnLeft <= time) r.Lost++;
        if (s.Tendo > 0 && s.Tendo <= time) r.Lost++;
        if (s.UseOgi && s.Ogi > 0 && s.Ogi <= time) r.Lost++;
        if (s.UseZanshin && s.Zanshin > 0 && s.Zanshin <= time) r.Lost++;
        s.Time += time;
        s.GcdLeft = Math.Max(0, s.GcdLeft - time);
        if (s.BattleTime >= 0) s.BattleTime += time;
        s.Eye = Math.Max(0, s.Eye - time);
        s.TrueNorth = Math.Max(0, s.TrueNorth - time);
        s.ComboLeft = Math.Max(0, s.ComboLeft - time);
        s.Moon = Math.Max(0, s.Moon - time); s.Flower = Math.Max(0, s.Flower - time); s.Dot -= time;
        s.MirrorLeft = Math.Max(0, s.MirrorLeft - time); if (s.MirrorLeft == 0) s.MirrorStacks = 0;
        if (s.UseMirror) r.MirrorWaste += Math.Max(0, time - (2 - s.MirrorCharges) * 55);
        s.MirrorCharges = Math.Min(2, s.MirrorCharges + time / 55);
        s.Tendo = Math.Max(0, s.Tendo - time); s.ReturnLeft = Math.Max(0, s.ReturnLeft - time);
        s.Ogi = Math.Max(0, s.Ogi - time); s.Zanshin = Math.Max(0, s.Zanshin - time);
        s.IkiCd = Math.Max(0, s.IkiCd - time); s.SeneiCd = Math.Max(0, s.SeneiCd - time);
        s.ShohaCd = Math.Max(0, s.ShohaCd - time); s.ShintenCd = Math.Max(0, s.ShintenCd - time); s.ZanshinCd = Math.Max(0, s.ZanshinCd - time);
        s.Potion = Math.Max(0, s.Potion - time); s.Party = Math.Max(0, s.Party - time);
    }
}
