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
    public float Time;
    public int SenCount => ((Sen & 1) != 0 ? 1 : 0) + ((Sen & 2) != 0 ? 1 : 0) + ((Sen & 4) != 0 ? 1 : 0);
}

internal sealed class Samurai100Forecast
{
    public bool Computed, CurrentOnly;
    public SamuraiPrediction Prediction;
    public uint Gcd, OffGcd;
    public int Big, Tendo, Ogi, MirrorSnow, Mirrors, Lost;
    public bool OldReturn;
    public float Score, WindowScore, PotionAt = -1, EndTime;
    public string Trace = "";
    public int Tier => Big >= 7 && Ogi >= 2 && OldReturn ? (Tendo >= 4 ? 2 : Tendo >= 2 ? 1 : 0) : 0;
    public string Summary => CurrentOnly ? "后续时机未定，只判断当前出招" :
        $"{(Tier == 2 ? "双天道七刀" : Tier == 1 ? "常规七刀" : "当前资源输出")} 剩余预计大招={Big} 天道={Tendo} 明镜雪={MirrorSnow} 损失={Lost}";
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
        if (state.Moving || state.Distance > state.IaiRange) return Current(state, potionWait >= 0, trace);
        Samurai100Forecast? best = null;
        var candidates = potionWait < 0 ? 1 : Math.Min(16, 2 + (int)(potionWait / state.Gcd));
        for (var delay = 0; delay < candidates; delay++)
        for (var order = 0; order < 12; order++)
        {
            var candidate = Run(state, window, potionWait, delay * state.Gcd, order, trace);
            if (candidate.CurrentOnly) continue;
            if (potionWait >= 0 && candidate.PotionAt < 0) continue;
            if (best == null || Better(candidate, best)) best = candidate;
        }
        return best ?? Current(state, potionWait >= 0, trace);
    }

    private static Samurai100Forecast Current(Samurai100State s, bool beforePotion, bool trace)
    {
        var result = new Samurai100Forecast { Computed = true, CurrentOnly = true };
        if (s.GcdLeft >= AbilityLock) result.OffGcd = ChooseOff(s, false, beforePotion);
        Advance(ref s, s.GcdLeft, result);
        result.Gcd = ChooseGcd(s, 0, beforePotion, s.MirrorStacks > 0 && (s.Sen & 1) == 0);
        if (trace) result.Trace = $"当前GCD={result.Gcd} 当前穿插={result.OffGcd}";
        result.EndTime = s.Time;
        return result;
    }

    private static bool Better(Samurai100Forecast next, Samurai100Forecast old)
    {
        // 不用断花、丢预备或明镜雪换一个好看的七刀。
        if (next.Lost != old.Lost) return next.Lost < old.Lost;
        if (next.MirrorSnow != old.MirrorSnow) return next.MirrorSnow < old.MirrorSnow;
        var difference = next.Score - old.Score;
        if (Math.Abs(difference) > 1) return difference > 0;
        return next.PotionAt >= 0 && next.PotionAt < old.PotionAt;
    }

    private static Samurai100Forecast Run(Samurai100State s, float window, float wait, float notBefore, int order, bool trace)
    {
        var result = new Samurai100Forecast { Computed = true };
        var end = wait >= 0 ? wait + 30 : window;
        // 同一段后续一起比较，不能把明镜雪藏到窗口外。
        var horizon = end + 20;
        var start = wait >= 0 ? float.PositiveInfinity : s.WindowStart;
        var oldReturn = s.ReturnIsOld && s.ReturnLeft > 0;
        var maxMirrors = order / 4;
        var recoverSnow = s.MirrorStacks > 0 && (s.Sen & 1) == 0;
        var first = true;
        var gcds = 0;
        uint other = 0;
        var predict = SAMSettings.Instance.显示技能预测 || SAMSettings.Instance.显示下G横幅;
        var initialGcd = s.GcdLeft;
        Weave(ref s, result, initialGcd, wait, notBefore, ref start, ref end, maxMirrors, true, trace);
        Advance(ref s, Math.Max(0, initialGcd - s.Time), result);
        for (var step = 0; step < 64 && s.Time + Margin < horizon; step++)
        {
            var action = ChooseGcd(s, order, s.Time < start, recoverSnow);
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
            if (action == SAMSkill.燕回返) oldReturn = false;
            var next = at + s.Gcd;
            Advance(ref s, Math.Min(s.Gcd, cast > 0 ? cast + Margin : AbilityLock), result);
            Weave(ref s, result, next, wait, notBefore, ref start, ref end, maxMirrors, false, trace);
            Advance(ref s, Math.Max(0, next - s.Time), result);
        }
        result.EndTime = s.Time;
        result.Computed = !first;
        // 药外比较也保留后续明镜和闪的价值。
        result.Score += s.SenCount * 100 + s.MirrorCharges * 350 + s.MirrorStacks * 80;
        return result;
    }

    private static void Weave(ref Samurai100State s, Samurai100Forecast r, float until, float wait, float notBefore,
        ref float start, ref float end, int maxMirrors, bool current, bool trace)
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
            var mirrorFits = s.Time + (Math.Max(0, 3 - s.SenCount) + 1) * s.Gcd + s.Cast < end;
            var action = ChooseOff(s, r.Mirrors < maxMirrors && mirrorFits, preparingPotion);
            if (current && slot == 0) r.OffGcd = action;
            if (action == 0) break;
            if (trace) r.Trace += $"{s.Time:F2}:能力{action} ";
            ApplyOff(ref s, action, r, s.Time + Margin >= start && s.Time + Margin < end);
            Advance(ref s, AbilityLock, r);
        }
    }

    private static uint ChooseGcd(Samurai100State s, int order, bool beforePotion, bool recoverSnow)
    {
        var cast = s.Cast + Margin;
        var canCast = !s.Moving && s.Distance <= s.IaiRange;
        var hasReturn = s.ReturnLeft > Margin && s.Distance <= s.IaiRange;
        if (s.OgiReturn && s.Distance <= s.OgiRange) return SAMSkill.回返斩浪;
        if (hasReturn && s.Immediate) return SAMSkill.燕回返;
        if (s.UseDot && s.SenCount == 1 && s.Moon > cast && canCast && s.Dot <= cast + s.Gcd)
            return SAMSkill.彼岸花;
        if (hasReturn && s.ReturnLeft <= 2 * s.Gcd + Margin) return SAMSkill.燕回返;
        var ogi = s.UseOgi && s.Ogi > s.OgiCast + Margin && !s.Moving && s.Distance <= s.OgiRange && s.Moon > s.OgiCast;
        var flowerSafe = !s.UseDot || s.Dot > (ToFlower(s) + 2) * s.Gcd + cast;
        var returnSafe = Samurai100Rules.ReturnBeforeFlower(s);
        if (ogi && flowerSafe && s.Ogi <= 2 * s.Gcd + s.OgiCast) return SAMSkill.奥义斩浪;
        if (s.SenCount == 3 && canCast && s.Moon > cast)
        {
            // 为待吃药腾出一个瞬发插入位，最多垫前两刀。
            if (beforePotion && !s.Immediate && s.MirrorStacks == 0 && (order & 2) != 0 && s.Distance <= s.MeleeRange)
            {
                if (s.Combo == 0) return SAMSkill.晓风;
                if (s.Combo == SAMSkill.晓风) return s.Moon <= s.Flower ? SAMSkill.阵风 : SAMSkill.士风;
            }
            if (hasReturn) return SAMSkill.燕回返;
            if (ogi && flowerSafe && (order & 1) != 0 && s.Tendo <= 0) return SAMSkill.奥义斩浪;
            return SAMSkill.纷乱雪月花;
        }
        var mirrorSafe = s.MirrorStacks == 0 || s.MirrorLeft > (s.MirrorStacks + 2) * s.Gcd + Margin;
        if (hasReturn && !beforePotion && returnSafe && mirrorSafe &&
            (Samurai100Rules.Spending(s) || s.Potion > Margin || s.Party > Margin || s.Dump)) return SAMSkill.燕回返;
        if (ogi && flowerSafe && mirrorSafe && ((order & 1) != 0 || s.Tendo <= 0)) return SAMSkill.奥义斩浪;
        if (hasReturn && !beforePotion && returnSafe && ((order & 2) != 0 || s.Distance > s.MeleeRange)) return SAMSkill.燕回返;
        if (hasReturn && !beforePotion && returnSafe && s.MirrorStacks == 0 && KeepSen(s) &&
            (Combo(s) == SAMSkill.阵风 || Combo(s) == SAMSkill.士风)) return SAMSkill.燕回返;
        var waiting = Samurai100Rules.WaitingIaijutsu(s, KeepSen(s));
        if (waiting != 0 && (!canCast || waiting == SAMSkill.彼岸花 && s.MirrorStacks == 0 && s.Dot > cast + 2 * s.Gcd)) return 0;
        if (s.Distance > s.MeleeRange) return hasReturn ? SAMSkill.燕回返 : 0;
        if (s.MirrorStacks > 0)
        {
            if (s.SenCount == 3) return hasReturn ? SAMSkill.燕回返 : 0;
            if (s.SenCount == 1 && KeepSen(s))
            {
                if (hasReturn && s.Dot > cast + s.Gcd) return SAMSkill.燕回返;
                return canCast && s.Moon > cast ? SAMSkill.彼岸花 : 0;
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
            if (s.SenCount == 3) return hasReturn ? SAMSkill.燕回返 : 0;
            if (keep) return s.Dot <= cast + 2 * s.Gcd && canCast && s.Moon > cast ? SAMSkill.彼岸花 : 0;
            var bit = combo == SAMSkill.阵风 ? 2 : 4;
            return (s.Sen & bit) == 0 ? bit == 2 ? SAMSkill.月光 : SAMSkill.花车 : SAMSkill.晓风;
        }
        if (combo != SAMSkill.晓风) return SAMSkill.晓风;
        if (s.Moon <= 0 && s.Flower <= 0 && s.SenCount == 0) return SAMSkill.士风;
        if (s.Moon <= cast + 3 * s.Gcd && (s.Moon <= s.Flower || keep || s.SenCount == 3)) return SAMSkill.阵风;
        if (s.Flower <= cast + 3 * s.Gcd) return SAMSkill.士风;
        if (keep || s.SenCount == 3) return s.Moon <= s.Flower ? SAMSkill.阵风 : SAMSkill.士风;
        if ((s.Sen & 1) == 0) return SAMSkill.雪风;
        return Samurai100Rules.ChooseMoonFlower(s) == SAMSkill.月光 ? SAMSkill.阵风 : SAMSkill.士风;
    }

    private static uint Combo(Samurai100State s) => s.ComboLeft > Margin ? s.Combo : 0;

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
        return ToThree(s) + 1 + (s.ReturnLeft > 0 ? 1 : 0) + (s.MirrorStacks > 3 - s.SenCount ? 1 : 2) + (s.Immediate ? 1 : 0);
    }

    internal static bool KeepSen(Samurai100State s)
    {
        return s.UseDot && s.SenCount == 1 && s.Dot <= (ToThree(s) + 3 + (s.ReturnLeft > 0 ? 1 : 0)) * s.Gcd + s.Cast + Margin;
    }

    internal static float WaitSeconds(Samurai100State s)
    {
        if (!s.Moving && s.Distance <= s.IaiRange && s.MirrorStacks == 0 &&
            Samurai100Rules.WaitingIaijutsu(s, KeepSen(s)) == SAMSkill.彼岸花)
        {
            var seconds = s.Dot - (s.Cast + Margin + 2 * s.Gcd);
            if (seconds > 0) return seconds + .001f;
        }
        return float.PositiveInfinity;
    }

    internal static bool CanMirror(Samurai100State s)
    {
        if (!s.UseMirror || s.MirrorCharges < 1 || s.MirrorStacks > 0 || s.Tendo > 0 || Combo(s) != 0 || s.Distance > s.MeleeRange) return false;
        if (KeepSen(s)) return false;
        if (!s.Dump && (s.Sen & 1) == 0 && !(s.SenCount == 0 && s.UseDot && s.Dot <= 2 * s.Gcd + s.Cast)) return false;
        s.MirrorStacks = 3; s.MirrorLeft = 20; s.Tendo = 30;
        var check = new Samurai100Forecast();
        Advance(ref s, s.GcdLeft, check);
        // 三层全部有去处才开镜，连窗口外的收尾也检查。
        for (var step = 0; step < 12 && s.MirrorStacks > 0; step++)
        {
            var action = ChooseGcd(s, 0, false, false);
            if (action == 0 || action == SAMSkill.雪风 && !s.Dump) return false;
            if (action == SAMSkill.彼岸花 && s.Dot - s.Cast > 5) return false;
            ApplyGcd(ref s, action, check, false, false, s.Time + s.Cast + Margin);
            if (s.MirrorStacks == 0) return true;
            if (s.MirrorLeft <= s.Gcd + Margin) return false;
            Advance(ref s, s.Gcd, check);
        }
        return false;
    }

    internal static uint NextResourceGcd(Samurai100State s)
    {
        var check = new Samurai100Forecast();
        Advance(ref s, s.GcdLeft, check);
        return ChooseGcd(s, 0, false, true);
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

    internal static int IncomeBefore(Samurai100State s, float seconds)
    {
        var income = 0;
        var check = new Samurai100Forecast();
        var at = s.GcdLeft;
        Advance(ref s, at, check);
        for (var step = 0; step < 16 && at < seconds; step++)
        {
            var action = NextResourceGcd(s);
            if (action == 0) break;
            income += Samurai100Rules.KenkiGain(action);
            ApplyGcd(ref s, action, check, false, false, s.Time + s.Cast + Margin);
            Advance(ref s, s.Gcd, check);
            at += s.Gcd;
        }
        return income;
    }

    private static uint ChooseOff(Samurai100State s, bool mirror, bool beforePotion)
    {
        if (s.UseShoha && s.Meditation == 3 && s.ShohaCd <= 0 && s.Distance <= s.ShohaRange) return SAMSkill.照破;
        if (s.UseZanshin && s.Zanshin > 0 && s.ZanshinCd <= 0 && s.Kenki >= 50 && s.Distance <= s.OgiRange &&
            (!beforePotion || s.Zanshin < 2 * s.Gcd)) return SAMSkill.残心;
        if (!beforePotion && Samurai100Rules.CanIkishoten(s, out _)) return SAMSkill.意气冲天;
        if (s.UseSenei && s.SeneiCd <= 0 && s.Kenki >= 25 && s.Moon > 0 && s.Distance <= s.MeleeRange && !beforePotion) return SAMSkill.必杀剑_闪影;
        if (s.MaxWeaves == 1 && s.UseShinten && s.ShintenCd <= 0 && s.Kenki > 85 && s.Distance <= s.MeleeRange)
            return SAMSkill.必杀剑_震天;
        if (mirror && CanMirror(s)) return SAMSkill.明镜止水;
        if (Samurai100Rules.SpendKenki(s, out _))
            return SAMSkill.必杀剑_震天;
        return 0;
    }

    private static void Damage(ref Samurai100State s, Samurai100Forecast r, float potency, bool window, bool critical = false, float delay = Margin)
    {
        // 用一致的威力权重比较顺序，不宣称实际伤害百分比。
        var value = potency * (critical ? 1.5f : 1) * (s.Moon > delay ? 1.13f : 1);
        r.Score += value;
        if (window)
        {
            r.WindowScore += value;
            r.Score += value * (s.Potion > 0 ? 1 : s.BattleTime >= 0 && s.BattleTime + delay < 20 ? .35f : .25f);
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
            // 保留花的后续价值，并扣掉提前覆盖的部分。
            potency = 1200 - Math.Max(0, s.Dot - s.Cast) * 50 / 3;
            s.Dot = 60 + s.Cast; s.Sen = 0; meditation = true;
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
        { s.MirrorCharges--; s.MirrorStacks = 3; s.MirrorLeft = 20; s.Tendo = 30; r.Mirrors++; }
        if (action == SAMSkill.意气冲天)
        { s.Kenki = Math.Min(100, s.Kenki + 50); s.Ogi = 30; s.Zanshin = 30; s.IkiCd = 120; }
        if (action == SAMSkill.照破)
        { Damage(ref s, r, 640, window); s.Meditation = 0; s.ShohaCd = 1; }
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
        if (s.UseDot && s.Dot > 0 && s.Dot <= time) r.Lost++;
        if (s.UseOgi && s.Ogi > 0 && s.Ogi <= time) r.Lost++;
        if (s.UseZanshin && s.Zanshin > 0 && s.Zanshin <= time) r.Lost++;
        s.Time += time;
        s.GcdLeft = Math.Max(0, s.GcdLeft - time);
        if (s.BattleTime >= 0) s.BattleTime += time;
        s.Eye = Math.Max(0, s.Eye - time);
        s.TrueNorth = Math.Max(0, s.TrueNorth - time);
        s.ComboLeft = Math.Max(0, s.ComboLeft - time);
        s.Moon = Math.Max(0, s.Moon - time); s.Flower = Math.Max(0, s.Flower - time); s.Dot = Math.Max(0, s.Dot - time);
        s.MirrorLeft = Math.Max(0, s.MirrorLeft - time); if (s.MirrorLeft == 0) s.MirrorStacks = 0;
        s.MirrorCharges = Math.Min(2, s.MirrorCharges + time / 55);
        s.Tendo = Math.Max(0, s.Tendo - time); s.ReturnLeft = Math.Max(0, s.ReturnLeft - time);
        s.Ogi = Math.Max(0, s.Ogi - time); s.Zanshin = Math.Max(0, s.Zanshin - time);
        s.IkiCd = Math.Max(0, s.IkiCd - time); s.SeneiCd = Math.Max(0, s.SeneiCd - time);
        s.ShohaCd = Math.Max(0, s.ShohaCd - time); s.ShintenCd = Math.Max(0, s.ShintenCd - time); s.ZanshinCd = Math.Max(0, s.ZanshinCd - time);
        s.Potion = Math.Max(0, s.Potion - time); s.Party = Math.Max(0, s.Party - time);
    }
}
