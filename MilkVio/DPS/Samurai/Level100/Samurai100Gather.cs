using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Level100;

// 只在AOE凑闪开启时替换取闪路线。
internal static class Samurai100Gather
{
    public static bool IsAoe(uint action) => action == SAMSkill.风光 || action == SAMSkill.满月 || action == SAMSkill.樱花;

    public static uint Next(Samurai100State s)
    {
        if (!s.GatherSen || s.TargetUnavailable || s.MirrorStacks > 0 || s.SenCount == 3) return 0;
        var combo = s.ComboLeft > Samurai100Helper.EffectMargin ? s.Combo : 0;
        var snow = (s.Sen & 1) != 0;
        var choose = s;
        if (combo != SAMSkill.晓风) choose.NeedsPosition = false;
        var finisher = Samurai100Rules.ChooseMoonFlower(choose);
        if (combo == SAMSkill.风光 && finisher != 0)
            return s.Distance <= s.AoeRange ? finisher == SAMSkill.月光 ? SAMSkill.满月 : SAMSkill.樱花 : 0;
        if (combo == SAMSkill.晓风)
        {
            if (s.Distance > s.MeleeRange) return 0;
            if (s.Moon <= s.Cast + 2 * s.Gcd && (s.Moon <= s.Flower || finisher == 0)) return SAMSkill.阵风;
            if (s.Flower <= 2 * s.Gcd) return SAMSkill.士风;
            if (!snow) return SAMSkill.雪风;
            return finisher == SAMSkill.月光 ? SAMSkill.阵风 : finisher == SAMSkill.花车 ? SAMSkill.士风 : 0;
        }
        // 开镜前先备雪；即将续花也保留短连。
        var prepareMirror = s.UseMirror && !snow && s.Tendo <= 0 && s.MirrorCharges + 2 * s.Gcd / 55 >= 1;
        var prepareDot = s.UseDot && s.SenCount == 0 && s.Dot <= 3 * s.Gcd + s.Cast && s.Moon > 2 * s.Gcd + s.Cast;
        if (finisher == 0 || (prepareMirror || prepareDot) && s.Distance <= s.MeleeRange)
            return s.Distance <= s.MeleeRange ? SAMSkill.晓风 : 0;
        return s.Distance <= s.AoeRange ? SAMSkill.风光 : 0;
    }

    public static int ToThree(Samurai100State s)
    {
        var count = (3 - s.SenCount) * 2;
        var stacks = s.MirrorStacks;
        if ((s.Sen & 2) == 0 && stacks > 0) { count--; stacks--; }
        if ((s.Sen & 4) == 0 && stacks > 0) count--;
        if (s.MirrorStacks > 0 || s.ComboLeft <= Samurai100Helper.EffectMargin) return count;
        if (s.Combo == SAMSkill.晓风 && (s.Sen & 1) == 0 ||
            s.Combo == SAMSkill.阵风 && (s.Sen & 2) == 0 ||
            s.Combo == SAMSkill.士风 && (s.Sen & 4) == 0 ||
            s.Combo == SAMSkill.风光 && (s.Sen & 6) != 6) count--;
        return Math.Max(0, count);
    }
}
