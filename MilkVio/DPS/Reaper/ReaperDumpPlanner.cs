using System.Text;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper;

// 倾泻只比较眼前的消费链；普通留资源规则和输出窗口目标不进入这里。
public sealed class ReaperDumpPlanner
{
    private ReaperState _cached;
    private ReaperProjection.Advice? _advice;
    private long _until;
    private uint _baseline;
    private bool _off;
    public string Debug { get; private set; } = "";

    public void Clear() { _until = 0; _advice = null; Debug = ""; }

    internal static float ScoreDamage(float damage, int lostSoul, int lostShroud, float seconds)
        => (damage - 6 * lostSoul - 20 * lostShroud) * MathF.Exp(-seconds / 12);

    public static (uint Gcd, uint Off, string Reason) Build(ReaperState s)
    {
        uint gcd = 0, off = 0;
        if (!s.Alive || !s.HasTarget) return (0, 0, "倾泻：等待目标");
        if (s.Enshrouded > 0)
        {
            gcd = ReaperProjection.Filler(s);
            if (NeedsDesign(s) && gcd != ReaperSkill.收获月) gcd = ReaperSkill.死亡之影;
            if (s.Void >= 2 && s.Melee) off = ReaperSkill.夜游魂切割;
            else if (s.Oblatio > 0) off = ReaperSkill.祭性;
        }
        else if (s.Reavers > 0) gcd = s.Melee ? ReaperSkill.缢杀 : 0;
        else
        {
            if (s.ComboAtRisk(s.Gcd)) gcd = s.ComboNext;
            else if (NeedsDesign(s)) gcd = ReaperSkill.死亡之影;
            else if (s.Perfectio > 0) gcd = ReaperSkill.完人;
            else if (s.CanHarvest || ReaperBurstRecovery.HarvestNextGcd(s)) gcd = ReaperSkill.大丰收;
            else if (s.Soulsow && s.HarvestMoonQt) gcd = ReaperSkill.收获月;
            else gcd = ReaperProjection.Filler(s);

            if (gcd is not (ReaperSkill.死亡之影 or ReaperSkill.大丰收) && !s.ComboAtRisk(s.Gcd))
            {
                if (CanUse(s, ReaperSkill.夜游魂衣, true)
                    && (s.FreeEnshroud > 0 || s.Shroud >= 90 || !s.GluttonyQt || s.GluttonyCd > 0 || s.Soul < 50))
                    off = ReaperSkill.夜游魂衣;
                else if (CanUse(s, ReaperSkill.暴食, true)) off = ReaperSkill.暴食;
                else if (CanUse(s, ReaperSkill.夜游魂衣, true)) off = ReaperSkill.夜游魂衣;
                else if (CanUse(s, ReaperSkill.隐匿挥割, true)) off = ReaperSkill.隐匿挥割;
            }
        }
        if (CanUse(s, ReaperSkill.神秘环, true)) off = ReaperSkill.神秘环;
        if (!CanChoose(s, gcd, false)) gcd = 0;
        if (!CanUse(s, off, true)) off = 0;
        return (gcd, off, s.Enshrouded > 0 && s.Lemure == 1 && s.Moving
            ? "倾泻：移动中保留团契，尝试收获月" : "倾泻：兑现当前资源");
    }

    private static bool NeedsDesign(ReaperState s)
    {
        if (!s.DotQt || !s.Melee || s.Reavers > 0) return false;
        var coverage = Math.Max(s.Gcd, 2);
        if (s.Enshrouded > 0)
            coverage = Math.Max(0, s.Lemure - 1) * s.ReapGcd + s.CommunioCast + 0.5f;
        else if (s.CanEnshroud) coverage = s.SingleDuration + 1;
        else if (s.GluttonyQt && s.GluttonyCd <= s.Gcd && s.Soul >= 50) coverage = 3 * s.Gcd + 0.5f;
        return s.DeathDesign <= s.GcdLeft + coverage;
    }

    internal static bool CanUse(ReaperState s, uint id, bool off)
    {
        if (!ReaperProjection.CanUse(s, id, off)) return false;
        if (id is ReaperSkill.夜游魂衣 or ReaperSkill.暴食 or ReaperSkill.隐匿挥割)
        {
            if (NeedsDesign(s) || s.ComboAtRisk(s.Gcd)) return false;
            var delay = id == ReaperSkill.夜游魂衣 ? s.SingleDuration : id == ReaperSkill.暴食 ? 2 * s.Gcd : s.Gcd;
            if (s.Perfectio > 0 && s.Perfectio <= s.GcdLeft + delay + s.Gcd) return false;
        }
        // 满红不拿切割当纯伤害技；少量溢红交给整条消费链比较。
        if (id == ReaperSkill.灵魂切割 && s.Soul > 60) return false;
        if (id == ReaperSkill.死亡之影) return NeedsDesign(s);
        return true;
    }

    private static bool CanChoose(ReaperState s, uint id, bool off) => CanUse(s, id, off)
        || !off && id == ReaperSkill.大丰收 && ReaperBurstRecovery.HarvestNextGcd(s);

    // 推演失败、技能被游戏拒绝时仍从倾泻候选中继续，不能回到普通留红/留绿求解器。
    internal static IEnumerable<uint> Candidates(ReaperState s, bool off)
    {
        var plan = Build(s);
        yield return off ? plan.Off : plan.Gcd;
        if (off)
        {
            yield return ReaperSkill.神秘环;
            yield return ReaperSkill.夜游魂切割;
            yield return ReaperSkill.祭性;
            yield return ReaperSkill.夜游魂衣;
            yield return ReaperSkill.暴食;
            yield return ReaperSkill.隐匿挥割;
            if (plan.Gcd is ReaperSkill.完人 or ReaperSkill.大丰收 or ReaperSkill.收获月
                || s.GluttonyQt && s.GluttonyCd > 0 && s.GluttonyCd <= 2 * s.Gcd) yield return 0;
        }
        else
        {
            yield return ReaperSkill.完人;
            yield return ReaperSkill.大丰收;
            yield return ReaperSkill.收获月;
            yield return ReaperSkill.灵魂切割;
            yield return ReaperProjection.Filler(s);
            yield return s.ComboNext != 0 ? s.ComboNext : ReaperSkill.切割;
            yield return ReaperSkill.死亡之影;
        }
    }

    public ReaperProjection.Advice? Choose(ReaperBurstPlanner planner, ReaperState s, uint gcd, uint off, bool evaluate)
    {
        var isOff = s.GcdLeft > 0.3f;
        var baseline = isOff ? off : gcd;
        if (ReaperProjection.SameDecision(s, _cached) && _off == isOff && _baseline == baseline && s.Now < _until)
            return _advice.HasValue && CanChoose(s, _advice.Value.Action, isOff) ? _advice : null;
        if (!evaluate) return null;
        _cached = s; _off = isOff; _baseline = baseline; _until = s.Now + 150; _advice = null;
        Debug = "";
        if (!s.HasTiming || s.Level < 100 || isOff && s.GcdLeft <= 0.65f) return null;
        if (!isOff && (s.Locked || s.ComboAtRisk(s.Gcd) || gcd == ReaperSkill.死亡之影
            || s.Perfectio > 0 && s.Perfectio <= s.GcdLeft + s.Gcd + 0.5f)) return null;
        if (isOff && (s.Locked || off == ReaperSkill.神秘环 || gcd == ReaperSkill.死亡之影 || s.ComboAtRisk(s.Gcd))) return null;

        var horizon = Math.Clamp(2 * s.SingleDuration + 3 * s.Gcd, 24, 30);
        var best = ReaperProjection.Forecast(planner, s, baseline, isOff, horizon, dump: true);
        if (!best.Complete) { Debug = "倾泻推演未完成，使用倾泻基础规则"; return null; }
        var selected = baseline;
        var detail = new StringBuilder($"倾泻比较 {horizon:F1}s；尾部仅完成已获得的技能\n");
        void Describe(uint id, ReaperProjection.Result result) => detail.AppendLine(
            $"{id}: 完成={result.Complete} 近期收益={result.DumpScore:F0} 威力={result.Damage:F0} 团契={result.Communios} 暴食={result.Gluttonies} 完人={result.Perfectios} 溢出={result.LostSoul}/{result.LostShroud}");
        Describe(baseline, best);
        foreach (var id in Candidates(s, isOff).Distinct())
        {
            if (id == baseline || !CanChoose(s, id, isOff)) continue;
            var result = ReaperProjection.Forecast(planner, s, id, isOff, horizon, dump: true);
            Describe(id, result);
            if (result.Complete && result.DumpScore > best.DumpScore + 20)
            { best = result; selected = id; }
        }
        Debug = detail.ToString();
        if (selected != baseline) _advice = new(selected, isOff, "倾泻：比较近期消费链后的收益");
        return _advice;
    }
}
