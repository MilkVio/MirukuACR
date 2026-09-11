using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper;

// 有限候选的实时比较；预测只运行90级规划器，不调用100级模拟或消费链。
internal sealed class ReaperLevel90Projection
{
    internal readonly record struct Advice(uint Id, bool Off, string Reason);
    internal readonly record struct Result(bool Complete, float Damage, float BuffDamage, float QuickScore,
        int Slices, int Gluttonies, int Communios, int BuffCommunios, int LostSoul, int LostShroud, ReaperState End);
    private ReaperState _cached;
    private long _until;
    private bool _off;
    private uint _baseline;
    private Advice? _advice;
    public string Debug { get; private set; } = "";
    public void Clear() { _until = 0; _advice = null; Debug = ""; }

    internal Advice? Choose(ReaperLevel90Planner planner, ReaperState s, uint gcd, uint off)
    {
        var closing = ReaperResources.IsWindowClosing(s);
        var burst = s.CircleLeft > 0;
        var reserve = s.WindowActive && (s.GoalSoul > 0 || s.GoalShroud > 0);
        var isOff = s.GcdLeft > .3f;
        if (s.CastingCommunio || !s.HasTiming || isOff && s.GcdLeft <= .65f
            || planner.IsPlanned && !reserve || !s.IsDump && !s.WindowActive && !burst && s.Shroud < 70 && s.Soul < 90)
            return null;
        if (s.ComboAtRisk(s.Gcd) || gcd == ReaperSkill.死亡之影 || isOff && off == ReaperSkill.神秘环) return null;
        // 已经获得的大丰收奖励优先兑现。只有实际输出截止/期末目标才改变这条链。
        if (!closing && !reserve && (gcd == ReaperSkill.大丰收 || s.EnshroudQt && s.FreeEnshroud > 0)) return null;
        var baseline = isOff ? off : gcd != 0 ? gcd : Filler(s);
        if (_cached.Level == s.Level && ReaperProjection.SameDecision(s, _cached) && _off == isOff
            && (s.PositionalBuff > 0) == (_cached.PositionalBuff > 0) && (s.ReapingBuff > 0) == (_cached.ReapingBuff > 0)
            && _baseline == baseline && s.Now < _until)
            return _advice.HasValue && CanChoose(s, _advice.Value.Id, isOff) ? _advice : null;
        _cached = s; _off = isOff; _baseline = baseline; _until = s.Now + 150; _advice = null;
        var horizon = s.WindowActive ? s.WindowLeft : s.IsDump ? Math.Clamp(2 * s.SingleDuration + 3 * s.Gcd, 24, 30)
            : burst ? s.CircleLeft : Math.Clamp(s.CircleCd + 145, 145, 265);
        if (!float.IsFinite(horizon) || horizon <= 0 || horizon > 600) { Debug = "使用90级基础规则"; return null; }
        var original = Forecast(planner, s, baseline, isOff, horizon);
        if (!original.Complete) { Debug = "推演未完成，保留基础决策"; return null; }
        var best = original; var selected = baseline;
        foreach (var id in Candidates(s, isOff).Prepend(0u).Distinct())
        {
            if (id == baseline || !CanChoose(s, id, isOff)) continue;
            if (!s.IsDump && !closing && id == ReaperSkill.夜游魂衣 && s.CircleLeft <= 0
                && ReaperResources.ShouldHoldEnshroud(s, out _)) continue;
            if (!s.IsDump && !s.WindowActive && id == ReaperSkill.灵魂切割 && s.Soul > 50) continue;
            if (id == ReaperSkill.死亡之影 && gcd != id) continue;
            if (id is ReaperSkill.夜游魂衣 or ReaperSkill.暴食 or ReaperSkill.隐匿挥割
                && gcd == ReaperSkill.大丰收 && !closing) continue;
            var result = Forecast(planner, s, id, isOff, horizon);
            if (!result.Complete) continue;
            if (id == ReaperSkill.灵魂切割 && s.Soul > 50 && s.WindowActive && result.Slices <= original.Slices) continue;
            if (!s.IsDump && !s.WindowActive && !burst && result.BuffCommunios < original.BuffCommunios) continue;
            if (Better(s, result, best, burst)) { best = result; selected = id; }
        }
        Debug = $"90级比较 {horizon:F1}s：建议{selected} 威力{best.Damage:F0} 团契{best.Communios}/团辅内{best.BuffCommunios}"
            + $" 切割{best.Slices} 暴食{best.Gluttonies} 期末红绿{best.End.Soul}/{best.End.Shroud} 溢出{best.LostSoul}/{best.LostShroud}";
        if (selected != baseline)
        {
            var reason = s.IsDump ? "倾泻：比较90级近期消费链" : s.WindowActive ? "窗口：比较90级资源安排" : "比较90级爆发收益";
            if (isOff && (selected is ReaperSkill.隐匿挥割 or ReaperSkill.暴食 || baseline is ReaperSkill.隐匿挥割 or ReaperSkill.暴食))
                reason += $" {baseline}→{selected} 暴食保留/消费={ReaperResources.EstimateGluttony(s, false).Time:F2}/{ReaperResources.EstimateGluttony(s, true).Time:F2}s";
            _advice = new(selected, isOff, reason);
        }
        return _advice;
    }

    private static bool Better(ReaperState s, Result a, Result b, bool burst)
    {
        if (s.WindowActive)
        {
            (float Worst, float Sum) Deficit(Result r)
            {
                var red = Math.Max(0, s.GoalSoul - r.End.Soul) / (float)Math.Max(1, s.GoalSoul);
                var green = Math.Max(0, s.GoalShroud - r.End.Shroud) / (float)Math.Max(1, s.GoalShroud);
                return (Math.Max(red, green), red + green);
            }
            var x = Deficit(a); var y = Deficit(b);
            if (Math.Abs(x.Worst - y.Worst) > .001f) return x.Worst < y.Worst;
            if (Math.Abs(x.Sum - y.Sum) > .001f) return x.Sum < y.Sum;
        }
        if (s.IsDump) return a.QuickScore > b.QuickScore + 20;
        if (burst && !s.WindowActive && Math.Abs(a.BuffDamage - b.BuffDamage) > .5f) return a.BuffDamage > b.BuffDamage;
        if (Math.Abs(a.Damage - b.Damage) > .5f) return a.Damage > b.Damage;
        return a.LostSoul + a.LostShroud < b.LostSoul + b.LostShroud;
    }

    internal static IEnumerable<uint> Candidates(ReaperState s, bool off)
    {
        if (off)
        {
            yield return ReaperSkill.神秘环; yield return ReaperSkill.夜游魂切割;
            if (ReaperLevelRules.HasSacrificium(s.Level)) yield return ReaperSkill.祭性;
            yield return ReaperSkill.夜游魂衣; yield return ReaperSkill.暴食; yield return ReaperSkill.隐匿挥割;
        }
        else
        {
            yield return ReaperSkill.大丰收; yield return ReaperSkill.收获月; yield return ReaperSkill.灵魂切割;
            yield return Filler(s); yield return ReaperLevel90Planner.Combo(s); yield return ReaperSkill.死亡之影;
        }
    }

    internal static uint Filler(ReaperState s)
    {
        if (s.Enshrouded > 0)
        {
            if (s.Lemure > 1) return s.Melee ? ReaperSkill.虚无收割 : s.CanHarvestMoonForRange ? ReaperSkill.收获月 : 0;
            if (s.Lemure == 1 && !s.Moving && s.Void == 0 && s.Oblatio <= 0) return ReaperSkill.团契;
            return s.CanHarvestMoonForRange ? ReaperSkill.收获月 : 0;
        }
        if (s.Reavers > 0) return s.Melee ? ReaperSkill.缢杀 : 0;
        if (!s.Melee) return ReaperResources.AllowsHarvestMoon(s) ? ReaperSkill.收获月 : s.HarpeQt && !s.Moving ? ReaperSkill.勾刃 : 0;
        if (s.ComboAtRisk(s.Gcd)) return s.ComboNext;
        if (s.SliceQt && s.SliceCharges >= 1 && s.Soul <= 50) return ReaperSkill.灵魂切割;
        return ReaperLevel90Planner.Combo(s);
    }

    internal static bool CanUse(ReaperState s, uint id, bool off)
    {
        if (!ReaperLevelRules.UsesLevel90(s.Level) || !ReaperLevelRules.Learned(s.Level, id)) return false;
        if (!ReaperProjection.CanUse(s, id, off)) return false;
        if (id == ReaperSkill.神秘环 && s.Enshrouded > 0 && s.Lemure == 1 && (s.Void > 0 || s.Oblatio > 0)) return false;
        if (id == ReaperSkill.团契 && (s.Void > 0 || s.Oblatio > 0)) return false;
        if (id == ReaperSkill.灵魂切割 && s.Soul > (s.WindowActive ? 90 : s.IsDump ? 60 : 50)) return false;
        return true;
    }
    private static bool CanChoose(ReaperState s, uint id, bool off)
    {
        // 候选比较与缓存命中都要遵守暴食保护，不能仅因红高就覆盖基础判断。
        if (id == ReaperSkill.隐匿挥割 && !s.IsDump && !ReaperResources.IsWindowClosing(s)
            && ReaperResources.ShouldHoldBlood(s, out _)) return false;
        return CanUse(s, id, off) || !off && id == ReaperSkill.大丰收 && ReaperBurstRecovery.HarvestNextGcd(s);
    }

    internal static Result Forecast(ReaperLevel90Planner source, ReaperState initial, uint first, bool firstOff, float horizon)
        => new Simulation(source.Fork(), initial).Run(first, firstOff, horizon);

    private sealed class Simulation
    {
        private readonly ReaperLevel90Planner _planner;
        private readonly long _started;
        private ReaperState _s;
        private float _time, _lock, _damage, _buffDamage, _quickScore;
        private int _slices, _gluts, _coms, _buffComs, _lostSoul, _lostShroud;
        internal Simulation(ReaperLevel90Planner planner, ReaperState s)
        { _planner = planner; _s = ReaperLevelRules.Normalize(s) with { Moving = false }; _started = s.Now; }

        internal Result Run(uint first, bool firstOff, float horizon)
        {
            var forced = false; var complete = true;
            var spendingUntil = horizon;
            // 倾泻的各候选使用相同尾段，仅兑现已启动的链，不虚构下一轮资源。
            if (_s.IsDump) horizon += 2 * _s.SingleDuration + 2 * _s.Gcd + 2;
            for (var step = 0; step < 1600 && _time < horizon - .001f; step++)
            {
                _planner.Update(_s);
                var gcd = _s.GcdLeft <= .001f && _lock <= .001f;
                var off = _s.GcdLeft > .65f && _lock <= .001f && _s.GcdElapsed >= .65f;
                if (gcd || off)
                {
                    var forcing = !forced && firstOff == off;
                    var id = forcing ? first : off ? _planner.OffGcdAction : _planner.GcdAction != 0 ? _planner.GcdAction : Filler(_s);
                    // 首招0只放弃当前插入，CD转好等后续事件仍继续求解。
                    if (forcing) forced = true;
                    if (_s.IsDump && _time >= spendingUntil)
                    {
                        if (off && id is not (ReaperSkill.夜游魂切割 or ReaperSkill.祭性)
                            && !(id == ReaperSkill.夜游魂衣 && _s.FreeEnshroud > 0)) id = 0;
                        if (gcd && !_s.Locked && id is not (ReaperSkill.大丰收 or ReaperSkill.收获月 or ReaperSkill.死亡之影)) id = ReaperLevel90Planner.Combo(_s);
                    }
                    if (forcing && id != 0 && !CanUse(_s, id, off))
                    {
                        if (!off && id == ReaperSkill.大丰收 && ReaperBurstRecovery.HarvestNextGcd(_s)) forced = false;
                        else { complete = false; break; }
                    }
                    if (id != 0 && CanUse(_s, id, off))
                    {
                        if (_time + .15f >= horizon) { Advance(horizon - _time); break; }
                        if (!off)
                        {
                            var recast = id is ReaperSkill.虚无收割 or ReaperSkill.交错收割 ? _s.ReapGcd
                                : id == ReaperSkill.团契 ? _s.CommunioGcd : id == ReaperSkill.收获月 ? _s.HarvestMoonGcd : _s.Gcd;
                            var cast = id == ReaperSkill.团契 ? _s.CommunioCast : id == ReaperSkill.勾刃 ? _s.HarpeCast : 0;
                            _s = _s with { LastGcd = id, GcdLeft = recast, GcdElapsed = 0 };
                            if (cast > 0)
                            {
                                _s = _s with { CastingCommunio = id == ReaperSkill.团契 };
                                if (_time + cast + .15f >= horizon) { Advance(horizon - _time); break; }
                                Advance(cast); _planner.Update(_s);
                            }
                        }
                        Apply(id); _planner.ObserveAction(id); _lock = .65f; continue;
                    }
                }
                var next = horizon - _time;
                void At(float t) { if (t > .001f) next = Math.Min(next, t); }
                At(_lock); At(_s.GcdLeft); At(.65f - _s.GcdElapsed); At(_s.Gcd / 2 - _s.GcdElapsed);
                At(_s.CircleCd); At(_s.GluttonyCd); At(_s.EnshroudCd); At(_s.Bloodsown);
                Advance(Math.Max(.001f, next));
            }
            complete &= _time >= horizon - .01f && forced && (!_s.IsDump || !_s.Locked);
            return new(complete, _damage, _buffDamage, _quickScore, _slices, _gluts, _coms, _buffComs, _lostSoul, _lostShroud, _s);
        }

        private void Advance(float dt)
        {
            _time += dt; _lock = Math.Max(0, _lock - dt);
            float Dec(float t) => Math.Max(0, t - dt);
            _s = _s with
            {
                Now = _started + (long)(_time * 1000), GcdLeft = Dec(_s.GcdLeft), GcdElapsed = _s.GcdElapsed + dt,
                CircleCd = Dec(_s.CircleCd), GluttonyCd = Dec(_s.GluttonyCd), EnshroudCd = Dec(_s.EnshroudCd),
                CircleLeft = Dec(_s.CircleLeft), DeathDesign = Dec(_s.DeathDesign), ComboLeft = Dec(_s.ComboLeft),
                Bloodsown = Dec(_s.Bloodsown), SacrificeLeft = Dec(_s.SacrificeLeft), FreeEnshroud = Dec(_s.FreeEnshroud),
                Oblatio = Dec(_s.Oblatio), Enshrouded = Dec(_s.Enshrouded), PositionalBuff = Dec(_s.PositionalBuff), ReapingBuff = Dec(_s.ReapingBuff),
                SliceCharges = Math.Min(2, _s.SliceCharges + dt / Math.Max(1, _s.SliceRecast)),
                WindowLeft = Dec(_s.WindowLeft), WindowLimit = Dec(_s.WindowLimit)
            };
            if (_s.ComboLeft <= 0) _s = _s with { ComboNext = 0 };
            if (_s.SacrificeLeft <= 0) _s = _s with { SacrificeStacks = 0 };
            if (_s.Enshrouded <= 0) _s = _s with { Lemure = 0, Void = 0, Oblatio = 0, ReapingBuff = 0 };
        }
        private void AddSoul(int amount)
        { _lostSoul += Math.Max(0, _s.Soul + amount - 100); _s = _s with { Soul = Math.Min(100, _s.Soul + amount) }; }
        private void Apply(uint id)
        {
            var potencyId = id == ReaperSkill.缢杀 && _s.Executioner ? ReaperSkill.缢杀处刑
                : id == ReaperSkill.隐匿挥割 && _s.PositionalBuff > 0 ? ReaperSkill.缢杀爪 : id;
            var enhanced = id is ReaperSkill.虚无收割 or ReaperSkill.交错收割 ? _s.ReapingBuff > 0 : _s.PositionalBuff > 0;
            var potency = ReaperLevelRules.Potency(_s.Level, potencyId, enhanced,
                sacrifices: _s.SacrificeStacks);
            var damage = potency * (_s.DeathDesign > 0 ? 1.1f : 1) * (_s.CircleLeft > 0 ? 1.03f : 1);
            var inBuff = _s.CircleLeft > 0;
            var soulLoss = _lostSoul; var shroudLoss = _lostShroud;
            switch (id)
            {
                case ReaperSkill.神秘环:
                    _s = _s with { CircleCd = 120, CircleLeft = 20, Bloodsown = 6.7f, SacrificeStacks = 1, SacrificeLeft = 30 }; break;
                case ReaperSkill.夜游魂衣:
                    _s = _s with { Shroud = _s.Shroud - (_s.FreeEnshroud > 0 ? 0 : 50), FreeEnshroud = 0,
                        Enshrouded = 30, Lemure = 5, Void = 0, ReapingBuff = 0,
                        Oblatio = ReaperLevelRules.HasSacrificium(_s.Level) ? 30 : 0, EnshroudCd = 5 }; break;
                case ReaperSkill.暴食:
                    _gluts++; _s = _s with { Soul = _s.Soul - 50, Reavers = 2, Executioner = ReaperLevelRules.HasExecutioner(_s.Level), GluttonyCd = 60 }; break;
                case ReaperSkill.隐匿挥割:
                    _s = _s with { Soul = _s.Soul - 50, Reavers = 1, Executioner = false }; break;
                case ReaperSkill.缢杀:
                    _lostShroud += Math.Max(0, _s.Shroud + 10 - 100);
                    _s = _s with { Reavers = _s.Reavers - 1, Shroud = Math.Min(100, _s.Shroud + 10), PositionalBuff = 60 }; break;
                case ReaperSkill.死亡之影:
                    _s = _s with { DeathDesign = Math.Min(60, _s.DeathDesign + 30) }; break;
                case ReaperSkill.灵魂切割:
                    _slices++; _s = _s with { SliceCharges = Math.Max(0, _s.SliceCharges - 1) }; AddSoul(50); break;
                case ReaperSkill.虚无收割:
                case ReaperSkill.交错收割:
                    _s = _s with { Lemure = _s.Lemure - 1, Void = _s.Void + 1, ReapingBuff = 30 }; break;
                case ReaperSkill.夜游魂切割:
                    _s = _s with { Void = _s.Void - 2 }; break;
                case ReaperSkill.祭性:
                    _s = _s with { Oblatio = 0 }; break;
                case ReaperSkill.团契:
                    _coms++; if (inBuff) _buffComs++;
                    _s = _s with { Enshrouded = 0, Lemure = 0, Void = 0, Oblatio = 0, ReapingBuff = 0, CastingCommunio = false }; break;
                case ReaperSkill.大丰收:
                    _s = _s with { SacrificeStacks = 0, SacrificeLeft = 0, FreeEnshroud = 30 }; break;
                case ReaperSkill.收获月:
                    _s = _s with { Soulsow = false }; AddSoul(10); break;
                case ReaperSkill.勾刃: AddSoul(10); break;
                default:
                    _s = _s with { ComboNext = id == ReaperSkill.切割 ? ReaperSkill.增盈切割 : id == ReaperSkill.增盈切割 ? ReaperSkill.地狱切割 : 0,
                        ComboLeft = id == ReaperSkill.地狱切割 ? 0 : 30 }; AddSoul(10); break;
            }
            _damage += damage; if (inBuff) _buffDamage += damage;
            // 倾泻评分的经验机会成本，并非技能威力；时间折扣与100级保持同一12秒尺度。
            _quickScore += (damage - 6 * (_lostSoul - soulLoss) - 16 * (_lostShroud - shroudLoss)) * MathF.Exp(-_time / 12);
        }
    }
}
