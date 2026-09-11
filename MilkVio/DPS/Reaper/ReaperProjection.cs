using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper;

// 比较下一招及几种爆发衔接，后续复用现有规划器。模拟不接触游戏或时间轴。
public sealed class ReaperProjection
{
    public readonly record struct Advice(uint Action, bool OffGcd, string Reason)
    {
        internal ReaperBurstRoute Route { get; init; }
    }
    public readonly record struct Result(bool Complete, float Damage, int Slices, int Gluttonies,
        int Communios, int Perfectios, int BuffCommunios, int BuffPerfectios, int LostSoul, int LostShroud,
        float CappedSeconds, ReaperState End)
    {
        public float DumpScore { get; init; }
        public float BuffDamage { get; init; }
        public float? BuffPerfectioAt { get; init; }
        public float? BuffCommunioAt { get; init; }
    }

    private ReaperState _cached;
    private Advice? _advice;
    private bool _cachedOff;
    private long _until;
    private uint _gcd, _off;
    private ReaperBurstPhase _phase;
    public string Debug { get; private set; } = "";
    public float? BuffFinishIn { get; private set; }
    public float? BuffCommunioIn { get; private set; }
    public void Clear() { _until = 0; _advice = null; Debug = ""; BuffFinishIn = BuffCommunioIn = null; }

    public Advice? Choose(ReaperBurstPlanner planner, ReaperState s, uint gcd, uint off, bool evaluate)
    {
        var harvestPriority = !planner.IsPlanned && ReaperBurstRecovery.PrioritizeHarvestReward(s);
        if (harvestPriority && (!s.WindowActive || s.GoalSoul == 0 && s.GoalShroud == 0))
        {
            // 已解锁的爆发奖励按基础规划兑现，不为自己团辅末尾的一格填充延后整条链。
            Clear();
            Debug = s.FreeEnshroud > 0 ? "免费附体优先，不比较普通填充" : "大丰收优先，不比较普通填充";
            return null;
        }
        var isOff = s.GcdLeft > 0.3f;
        var burst = !planner.IsPlanned && ReaperBurstRecovery.IsActive(s);
        var closing = ReaperResources.IsWindowClosing(s);
        if (s.CastingCommunio) return null;
        if (isOff && s.GcdLeft <= 0.65f) return null;
        if (!burst && gcd == ReaperSkill.死亡之影 && (!s.WindowActive || s.WindowLeft > 20)) return null;
        if (SameDecision(s, _cached) && isOff == _cachedOff && s.Now < _until && gcd == _gcd && off == _off && planner.Phase == _phase)
            return _advice.HasValue && UsableAdvice(s, _advice.Value.Action, isOff) ? _advice : null;
        if (!evaluate) return null;
        _cached = s; _cachedOff = isOff; _until = s.Now + 150; _advice = null;
        _gcd = gcd; _off = off; _phase = planner.Phase;
        BuffFinishIn = BuffCommunioIn = null;
        if (s.Locked && !s.WindowActive && !burst) return null;
        var baseline = isOff ? off : gcd != 0 ? gcd : Filler(s);
        var horizon = burst
            ? s.CircleLeft > 0 ? s.CircleLeft : s.CircleCd + s.GcdLeft + 22
            : s.WindowActive ? s.WindowLeft : Math.Clamp(s.CircleCd + 145, 145, 265);
        if (s.WindowActive) horizon = Math.Min(horizon, s.WindowLeft);
        // 后窗口只处理眼前机会，不把容错上限当作保证的输出时间。
        if (horizon <= 0) horizon = Math.Min(s.WindowLimit, Math.Max(s.Gcd, 2));
        if (horizon <= 0 || horizon > 600) { Debug = "窗口过长，使用保守资源规则"; return null; }
        var candidates = new List<(uint Id, ReaperBurstRoute Route)> { (baseline, ReaperBurstRoute.当前安排) };
        void AddRoute(uint id, ReaperBurstRoute route)
        {
            if (s.WindowActive && !closing && id == ReaperSkill.夜游魂衣 && s.CircleLeft <= 0
                && ReaperResources.ShouldHoldEnshroud(s, out _)) return;
            if (planner.RecoveryRoute != ReaperBurstRoute.当前安排 && route != planner.RecoveryRoute) return;
            if (planner.RecoveryRoute != ReaperBurstRoute.当前安排 && isOff
                && ReaperBurstRecovery.Apply(s, planner.RecoveryRoute, gcd, id).Off != id) return;
            if (!candidates.Contains((id, route)) && UsableAdvice(s, id, isOff)) candidates.Add((id, route));
        }
        void Add(uint id)
        {
            if (isOff && off == ReaperSkill.神秘环 && id != off) return;
            if (id == ReaperSkill.死亡之影 && gcd != id) return;
            if (!s.WindowActive && id == ReaperSkill.夜游魂衣 && s.CircleLeft <= 0 && s.FreeEnshroud <= 0)
            {
                if (s.CircleQt && !ReaperResources.ForecastPreparation(s, true).Ready) return;
                if (s.GluttonyQt && s.GluttonyCd < 13 && s.Shroud <= 80) return;
            }
            AddRoute(id, planner.RecoveryRoute);
            if (burst) AddRoute(id, ReaperBurstRoute.大丰收优先);
        }
        if (burst)
        {
            foreach (var route in new[] { ReaperBurstRoute.大丰收优先, ReaperBurstRoute.付费附体优先, ReaperBurstRoute.暴食衔接大丰收 })
            {
                var choice = ReaperBurstRecovery.Apply(s, route, gcd, off);
                AddRoute(isOff ? choice.Off : choice.Gcd != 0 ? choice.Gcd : Filler(s), route);
            }
        }
        if (isOff)
        {
            Add(ReaperSkill.夜游魂衣); Add(ReaperSkill.暴食); Add(ReaperSkill.隐匿挥割);
            Add(ReaperSkill.祭性); Add(ReaperSkill.夜游魂切割);
            if (s.CircleQt && s.CircleCd <= 0) Add(ReaperSkill.神秘环);
            Add(0);
        }
        else
        {
            Add(Filler(s)); Add(s.ComboNext != 0 ? s.ComboNext : ReaperSkill.切割);
            Add(ReaperSkill.灵魂切割); Add(ReaperSkill.死亡之影);
            Add(ReaperSkill.大丰收); Add(ReaperSkill.完人); Add(ReaperSkill.收获月);
        }
        // 完整双附体和紧急连击仍使用原安排；窗口不足时由规划器先解除旧计划。
        if (planner.IsPlanned && !(s.WindowActive && (s.GoalSoul > 0 || s.GoalShroud > 0)))
            return null;
        if (!s.Locked && s.ComboAtRisk(s.Gcd) && !isOff) return null;
        var original = Forecast(planner, s, baseline, isOff, horizon);
        if (!original.Complete) { Debug = "联合推演未完成，使用基础规则"; return null; }
        // 团辅比较只看这一轮；期末目标仍在用户的名义截止验证。
        var reserve = s.WindowActive && (s.GoalSoul > 0 || s.GoalShroud > 0) && s.WindowLeft > horizon + 0.01f;
        if (reserve)
        {
            var end = Forecast(planner, s, baseline, isOff, s.WindowLeft);
            if (!end.Complete) return null;
            original = original with { End = end.End };
        }
        var best = original;
        var selected = baseline;
        var selectedRoute = planner.RecoveryRoute;
        var detail = new System.Text.StringBuilder();
        detail.AppendLine(Describe(baseline, original));
        foreach (var (id, route) in candidates.Skip(1))
        {
            // 无窗口时不以主动切割溢红换虚构的最后一次使用。
            if (!s.WindowActive && id == ReaperSkill.灵魂切割 && s.Soul > 50) continue;
            var result = ForecastBurst(planner, s, id, isOff, horizon, route);
            if (reserve)
            {
                var end = ForecastBurst(planner, s, id, isOff, s.WindowLeft, route);
                if (!end.Complete) continue;
                result = result with { End = end.End };
            }
            detail.AppendLine($"{route} {Describe(id, result)}");
            if (!result.Complete) continue;
            if (id == ReaperSkill.灵魂切割 && s.Soul > 50 && result.Slices <= original.Slices) continue;
            if (!s.WindowActive && !burst && result.BuffCommunios < original.BuffCommunios) continue;
            var a = Deficit(s, result.End); var b = Deficit(s, best.End);
            var reserveOrder = Math.Abs(a.Worst - b.Worst) > .001f ? a.Worst.CompareTo(b.Worst)
                : Math.Abs(a.Total - b.Total) > .001f ? a.Total.CompareTo(b.Total) : 0;
            // 免费链不按当前红绿拦截；只有名义截止的资源缺口更少时才改变这次衔接。
            if (reserveOrder < 0 || reserveOrder == 0 && !harvestPriority && (burst ? BetterBurst(result, best) : Better(s, result, best)))
            { best = result; selected = id; selectedRoute = route; }
        }
        Debug = detail.ToString();
        BuffFinishIn = best.BuffPerfectioAt;
        BuffCommunioIn = best.BuffCommunioAt;
        if (burst)
            _advice = new(selected, isOff,
                $"{(s.CircleLeft > 0 ? "剩余团辅" : "120衔接")}：{selectedRoute}，预计团契{best.BuffCommunios}/完人{best.BuffPerfectios}")
                { Route = selectedRoute };
        else if (selected != baseline)
        {
            var why = selected == ReaperSkill.灵魂切割 && s.Soul > 50
                ? $"提前切割：预计多{best.Slices - original.Slices}次，当前溢红{Math.Max(0, s.Soul - 50)}"
                : s.WindowActive ? $"窗口比较：预计留红{best.End.Soul}/绿{best.End.Shroud}"
                : "联合资源：比较附体、暴食和充能后的收益";
            _advice = new(selected, isOff, why);
        }
        else if (s.WindowActive && Deficit(s, best.End).Worst > 0)
            _advice = new(selected, isOff, $"期末目标暂不可达：预计留红{best.End.Soul}/绿{best.End.Shroud}");
        return _advice;
    }

    private static bool UsableAdvice(ReaperState s, uint id, bool off) => CanUse(s, id, off)
        || !off && id == ReaperSkill.大丰收 && ReaperBurstRecovery.HarvestNextGcd(s);

    private static bool BetterBurst(Result candidate, Result best)
    {
        if (Math.Abs(candidate.BuffDamage - best.BuffDamage) > 0.5f) return candidate.BuffDamage > best.BuffDamage;
        if (Math.Abs(candidate.Damage - best.Damage) > 0.5f) return candidate.Damage > best.Damage;
        return candidate.LostSoul + candidate.LostShroud < best.LostSoul + best.LostShroud;
    }

    internal static bool SameDecision(ReaperState a, ReaperState b) => a.PlayerId == b.PlayerId && a.TargetId == b.TargetId
        && a.Alive == b.Alive && a.InCombat == b.InCombat && a.HasTarget == b.HasTarget && a.CastingCommunio == b.CastingCommunio
        && a.WindowActive == b.WindowActive && a.WindowVersion == b.WindowVersion && a.GoalSoul == b.GoalSoul && a.GoalShroud == b.GoalShroud
        && a.Soul == b.Soul && a.Shroud == b.Shroud && a.Lemure == b.Lemure && a.Void == b.Void && a.Reavers == b.Reavers && a.Executioner == b.Executioner
        && a.LastGcd == b.LastGcd && a.ComboNext == b.ComboNext && a.Moving == b.Moving && a.Melee == b.Melee
        && a.CircleQt == b.CircleQt && a.EnshroudQt == b.EnshroudQt && a.GluttonyQt == b.GluttonyQt && a.BloodQt == b.BloodQt
        && a.FastCircle == b.FastCircle
        && a.SliceQt == b.SliceQt && a.DotQt == b.DotQt && a.HarvestMoonQt == b.HarvestMoonQt && a.HarpeQt == b.HarpeQt
        && a.DumpQt == b.DumpQt && a.FarPerfectioQt == b.FarPerfectioQt && a.Soulsow == b.Soulsow
        && (a.FreeEnshroud > 0) == (b.FreeEnshroud > 0) && (a.Perfectio > 0) == (b.Perfectio > 0)
        && (a.Enshrouded > 0) == (b.Enshrouded > 0) && (a.Oblatio > 0) == (b.Oblatio > 0)
        && (a.Occulta > 0) == (b.Occulta > 0) && a.SacrificeStacks == b.SacrificeStacks && (a.Bloodsown <= 0) == (b.Bloodsown <= 0)
        && (a.EnshroudCd <= 0) == (b.EnshroudCd <= 0) && (a.CircleLeft > 0) == (b.CircleLeft > 0)
        && (a.CircleCd <= 0) == (b.CircleCd <= 0) && (a.GluttonyCd <= 0) == (b.GluttonyCd <= 0)
        && Math.Abs(a.Bloodsown - b.Bloodsown) < 0.1f && Math.Abs(a.CircleLeft - b.CircleLeft) < 0.1f
        && (a.Bloodsown <= a.GcdLeft) == (b.Bloodsown <= b.GcdLeft)
        && ReaperBurstRecovery.HarvestNextGcd(a) == ReaperBurstRecovery.HarvestNextGcd(b)
        && (int)a.SliceCharges == (int)b.SliceCharges && Math.Abs(a.Gcd - b.Gcd) < 0.001f
        && Math.Abs(a.ReapGcd - b.ReapGcd) < 0.001f && Math.Abs(a.CommunioGcd - b.CommunioGcd) < 0.001f
        && Math.Abs(a.CommunioCast - b.CommunioCast) < 0.001f && Math.Abs(a.PerfectioGcd - b.PerfectioGcd) < 0.001f
        && Math.Abs(a.HarvestMoonGcd - b.HarvestMoonGcd) < 0.001f
        && Math.Abs(a.HarpeCast - b.HarpeCast) < 0.001f;

    private static (float Worst, float Total) Deficit(ReaperState s, ReaperState end)
    {
        var red = Math.Max(0, s.GoalSoul - end.Soul) / (float)Math.Max(1, s.GoalSoul);
        var green = Math.Max(0, s.GoalShroud - end.Shroud) / (float)Math.Max(1, s.GoalShroud);
        return (Math.Max(red, green), red + green);
    }

    private static bool Better(ReaperState s, Result candidate, Result best)
    {
        if (s.WindowActive)
        {
            var a = Deficit(s, candidate.End);
            var b = Deficit(s, best.End);
            var difference = a.Worst - b.Worst;
            if (Math.Abs(difference) > 0.001f) return difference < 0;
            difference = a.Total - b.Total;
            if (Math.Abs(difference) > 0.001f) return difference < 0;
        }
        else
        {
            // 不允许只把损失推给下一轮准备；剩余充能和红一起比较。
            if (!ReaperResources.ForecastPreparation(candidate.End, false).Ready
                && ReaperResources.ForecastPreparation(best.End, false).Ready) return false;
            if (candidate.End.Shroud + 10 < best.End.Shroud
                && candidate.Communios <= best.Communios) return false;
            if (candidate.End.Soul + candidate.End.SliceCharges * 50 + 10 < best.End.Soul + best.End.SliceCharges * 50
                && candidate.Gluttonies <= best.Gluttonies && candidate.Communios <= best.Communios) return false;
        }
        var margin = s.WindowActive ? 0.5f : 30;
        if (candidate.Damage > best.Damage + margin) return true;
        return Math.Abs(candidate.Damage - best.Damage) <= margin
            && candidate.LostSoul + candidate.LostShroud < best.LostSoul + best.LostShroud;
    }

    private static string Describe(uint id, Result r) => $"{id}: 完成={r.Complete} 威力={r.Damage:F0}/团辅{r.BuffDamage:F0} 切割={r.Slices} 暴食={r.Gluttonies} 团契={r.Communios}/{r.BuffCommunios} 完人={r.Perfectios}/{r.BuffPerfectios} 溢出={r.LostSoul}/{r.LostShroud} 满层={r.CappedSeconds:F1}s 期末={r.End.Soul}/{r.End.Shroud}";

    internal static uint Filler(ReaperState s)
    {
        if (s.Reavers > 0) return s.Melee ? ReaperSkill.缢杀 : 0;
        if (s.Enshrouded > 0) return s.Lemure > 1 ? s.Melee ? ReaperSkill.虚无收割
            : s.CanHarvestMoonForRange ? ReaperSkill.收获月 : 0
            : !s.Moving ? ReaperSkill.团契 : s.CanHarvestMoonForRange ? ReaperSkill.收获月 : 0;
        if (!s.Melee) return s.HarvestMoonQt && s.Soulsow ? ReaperSkill.收获月 : s.HarpeQt && !s.Moving ? ReaperSkill.勾刃 : 0;
        if (s.SliceQt && s.SliceCharges >= 1 && s.Soul <= 50 && !s.ComboAtRisk(s.Gcd)) return ReaperSkill.灵魂切割;
        return s.ComboNext != 0 ? s.ComboNext : ReaperSkill.切割;
    }

    internal static bool CanUse(ReaperState s, uint id, bool off) => id switch
    {
        0 => off,
        ReaperSkill.夜游魂衣 => s.CanEnshroud && s.Melee && !s.ComboAtRisk(ReaperResources.EnshroudComboDelay(s)),
        ReaperSkill.暴食 => s.GluttonyQt && s.GluttonyCd <= 0.001f && s.Soul >= 50 && !s.Locked && s.Distance <= 25 && !s.ComboAtRisk(2 * s.Gcd),
        ReaperSkill.隐匿挥割 => s.BloodQt && s.Soul >= 50 && !s.Locked && s.Melee && !s.ComboAtRisk(s.Gcd),
        ReaperSkill.神秘环 => s.CircleQt && s.CircleCd <= 0.001f,
        ReaperSkill.祭性 => s.Enshrouded > 0 && s.Oblatio > 0 && s.Distance <= 25,
        ReaperSkill.夜游魂切割 => s.Enshrouded > 0 && s.Void >= 2 && s.Melee,
        ReaperSkill.灵魂切割 => s.SliceQt && !s.Locked && s.Melee && s.SliceCharges >= 0.999f,
        ReaperSkill.死亡之影 => s.DotQt && s.Reavers == 0 && s.Melee,
        ReaperSkill.大丰收 => s.CanHarvest,
        ReaperSkill.完人 => ReaperResources.AllowsPerfectio(s),
        ReaperSkill.收获月 => ReaperResources.AllowsHarvestMoon(s),
        ReaperSkill.缢杀 => s.Reavers > 0 && s.Melee,
        ReaperSkill.虚无收割 or ReaperSkill.交错收割 => s.Enshrouded > 0 && s.Lemure > 1 && s.Melee,
        ReaperSkill.团契 => s.Enshrouded > s.CommunioCast + 0.15f && s.Lemure == 1 && !s.Moving && s.Distance <= 25,
        ReaperSkill.切割 => !s.Locked && s.Melee,
        ReaperSkill.增盈切割 or ReaperSkill.地狱切割 => !s.Locked && s.Melee && s.ComboNext == id && s.ComboLeft > s.GcdLeft,
        ReaperSkill.勾刃 => !s.Locked && s.HarpeQt && !s.Moving && s.Distance <= 25,
        _ => false
    };

    public static Result Forecast(ReaperBurstPlanner source, ReaperState initial, uint firstAction, bool firstOff, float horizon, bool dump = false)
        => new Simulation(source.ForkForForecast(), initial).Run(firstAction, firstOff, horizon, dump, ReaperBurstRoute.当前安排);

    internal static Result ForecastBurst(ReaperBurstPlanner source, ReaperState initial, uint firstAction, bool firstOff,
        float horizon, ReaperBurstRoute route)
        => new Simulation(source.ForkForForecast(), initial).Run(firstAction, firstOff, horizon, false, route);

    private sealed class Simulation
    {
        private readonly ReaperBurstPlanner planner;
        private readonly long _started;
        private ReaperState _s;
        private float _time, _lock, _damage, _capped, _dumpScore, _buffDamage;
        private float? _buffPerfectioAt, _buffCommunioAt;
        private bool _dump;
        private int _slices, _gluts, _coms, _perfects, _buffComs, _buffPerfects, _lostSoul, _lostShroud, _reaps;

        public Simulation(ReaperBurstPlanner source, ReaperState state)
        {
            // 只推演循环，当前移动不代表未来一直无法读条。真正派发仍检查实况。
            planner = source; _s = state with { Moving = false }; _started = state.Now;
            _reaps = state.Enshrouded > 0 ? Math.Max(0, 5 - state.Lemure) : 0;
        }

        public Result Run(uint first, bool firstOff, float horizon, bool dump, ReaperBurstRoute route)
        {
            _dump = dump;
            var spendingUntil = horizon;
            // 所有候选用同一个截止点和尾部长度。尾部不再开新的红绿消费链。
            if (dump) horizon += 2 * (4 * _s.ReapGcd + _s.CommunioGcd) + _s.PerfectioGcd + 2 * _s.Gcd + 2;
            var forced = false;
            var waitForGcd = false;
            var complete = true;
            for (var steps = 0; steps < 1600 && _time < horizon - 0.001f; steps++)
            {
                planner.Update(_s);
                var gcd = _s.GcdLeft <= 0.001f && _lock <= 0.001f;
                var off = !waitForGcd && _s.GcdLeft > 0.65f && _lock <= 0.001f && _s.GcdElapsed >= 0.65f;
                if (gcd) waitForGcd = false;
                uint id = 0;
                if (gcd || off)
                {
                    var forcing = !forced && firstOff == off;
                    if (forcing) { id = first; forced = true; waitForGcd = dump && firstOff && first == 0; }
                    else
                    {
                        var choice = planner.IsPlanned ? (Gcd: planner.GcdAction, Off: planner.OffGcdAction)
                            : ReaperBurstRecovery.Apply(_s, route, planner.GcdAction, planner.OffGcdAction);
                        id = off ? choice.Off : choice.Gcd != 0 ? choice.Gcd : Filler(_s);
                    }
                    if (dump && _time >= spendingUntil)
                    {
                        if (off && id is not (ReaperSkill.夜游魂切割 or ReaperSkill.祭性)
                            && !(id == ReaperSkill.夜游魂衣 && _s.FreeEnshroud > 0)) id = 0;
                        if (gcd && !_s.Locked && id is not (ReaperSkill.完人 or ReaperSkill.大丰收 or ReaperSkill.收获月 or ReaperSkill.死亡之影))
                            id = _s.Melee ? _s.ComboNext != 0 ? _s.ComboNext : ReaperSkill.切割 : 0;
                    }
                    if (forcing && id != 0 && !CanUse(_s, id, off))
                    {
                        // 首选大丰收尚差零点几秒时推进到真实解锁，既不插填充也不提前记伤害/免费附体。
                        if (!off && id == ReaperSkill.大丰收 && ReaperBurstRecovery.HarvestNextGcd(_s)) forced = false;
                        else { complete = false; break; }
                    }
                    if (id != 0 && CanUse(_s, id, off))
                    {
                        if (_time + 0.15f >= horizon) { Advance(horizon - _time); break; }
                        if (!off)
                        {
                            var recast = id is ReaperSkill.虚无收割 or ReaperSkill.交错收割 ? _s.ReapGcd
                                : id == ReaperSkill.团契 ? _s.CommunioGcd : id == ReaperSkill.完人 ? _s.PerfectioGcd
                                : id == ReaperSkill.收获月 ? _s.HarvestMoonGcd : _s.Gcd;
                            _s = _s with { GcdLeft = recast, GcdElapsed = 0, LastGcd = id };
                            var cast = id == ReaperSkill.团契 ? _s.CommunioCast : id == ReaperSkill.勾刃 ? _s.HarpeCast : 0;
                            if (cast > 0)
                            {
                                _s = _s with { CastingCommunio = id == ReaperSkill.团契 };
                                if (_time + cast + 0.15f >= horizon) { Advance(horizon - _time); break; }
                                Advance(cast);
                                planner.Update(_s);
                            }
                        }
                        Apply(id);
                        route = ReaperBurstRecovery.AfterAction(route, id);
                        planner.ObserveAction(id);
                        _lock = 0.65f;
                        continue;
                    }
                }
                var next = horizon - _time;
                void At(float t) { if (t > 0.001f) next = Math.Min(next, t); }
                At(_lock); At(_s.GcdLeft); At(0.65f - _s.GcdElapsed);
                At(_s.Gcd / 2 - _s.GcdElapsed); At(_s.CircleCd); At(_s.GluttonyCd); At(_s.EnshroudCd); At(_s.Bloodsown);
                Advance(Math.Max(0.001f, next));
            }
            if (_time < horizon - 0.01f || !forced) complete = false;
            if (dump && (_s.Locked || _s.Perfectio > 0)) complete = false;
            return new(complete, _damage, _slices, _gluts, _coms, _perfects, _buffComs, _buffPerfects, _lostSoul, _lostShroud, _capped, _s)
                { DumpScore = _dumpScore, BuffDamage = _buffDamage, BuffPerfectioAt = _buffPerfectioAt, BuffCommunioAt = _buffCommunioAt };
        }

        private void Advance(float dt)
        {
            _time += dt; _lock = Math.Max(0, _lock - dt);
            float Dec(float value) => Math.Max(0, value - dt);
            _capped += Math.Max(0, dt - (2 - _s.SliceCharges) * Math.Max(1, _s.SliceRecast));
            _s = _s with
            {
                Now = _started + (long)(_time * 1000), GcdLeft = Dec(_s.GcdLeft), GcdElapsed = _s.GcdElapsed + dt,
                CircleCd = Dec(_s.CircleCd), GluttonyCd = Dec(_s.GluttonyCd), EnshroudCd = Dec(_s.EnshroudCd),
                CircleLeft = Dec(_s.CircleLeft), DeathDesign = Dec(_s.DeathDesign), ComboLeft = Dec(_s.ComboLeft),
                Bloodsown = Dec(_s.Bloodsown), SacrificeLeft = Dec(_s.SacrificeLeft), FreeEnshroud = Dec(_s.FreeEnshroud),
                Occulta = Dec(_s.Occulta), Perfectio = Dec(_s.Perfectio), Oblatio = Dec(_s.Oblatio), Enshrouded = Dec(_s.Enshrouded),
                SliceCharges = Math.Min(2, _s.SliceCharges + dt / Math.Max(1, _s.SliceRecast)),
                WindowLeft = Dec(_s.WindowLeft), WindowLimit = Dec(_s.WindowLimit)
            };
            if (_s.ComboLeft <= 0) _s = _s with { ComboNext = 0 };
            if (_s.SacrificeLeft <= 0) _s = _s with { SacrificeStacks = 0 };
            if (_s.Enshrouded <= 0) _s = _s with { Lemure = 0, Void = 0, Oblatio = 0 };
        }

        private void AddSoul(int amount)
        {
            _lostSoul += Math.Max(0, _s.Soul + amount - 100);
            _s = _s with { Soul = Math.Min(100, _s.Soul + amount) };
        }
        private void Apply(uint id)
        {
            var potency = 0f;
            var oldSoulLoss = _lostSoul;
            var oldShroudLoss = _lostShroud;
            var inBuff = _s.CircleLeft > 0;
            var multiplier = (_s.DeathDesign > 0 ? 1.1f : 1) * (_s.CircleLeft > 0 ? 1.03f : 1);
            switch (id)
            {
                case ReaperSkill.神秘环:
                    _s = _s with { CircleCd = 120, CircleLeft = 20, Bloodsown = 6.7f, SacrificeStacks = 1, SacrificeLeft = 30 }; break;
                case ReaperSkill.夜游魂衣:
                    _s = _s with { Shroud = _s.Shroud - (_s.FreeEnshroud > 0 ? 0 : 50), FreeEnshroud = 0,
                        Enshrouded = 30, Lemure = 5, Void = 0, Oblatio = 30, EnshroudCd = 5 }; _reaps = 0; break;
                case ReaperSkill.暴食:
                    potency = 560; _gluts++;
                    _s = _s with { Soul = _s.Soul - 50, Reavers = 2, Executioner = true, GluttonyCd = 60 }; break;
                case ReaperSkill.隐匿挥割:
                    potency = 440; _s = _s with { Soul = _s.Soul - 50, Reavers = 1, Executioner = false }; break;
                case ReaperSkill.缢杀:
                    potency = _s.Executioner || _s.Reavers > 1 ? 820 : 620;
                    _lostShroud += Math.Max(0, _s.Shroud + 10 - 100);
                    _s = _s with { Reavers = _s.Reavers - 1, Shroud = Math.Min(100, _s.Shroud + 10) }; break;
                case ReaperSkill.死亡之影:
                    potency = 300; _s = _s with { DeathDesign = Math.Min(60, _s.DeathDesign + 30) }; break;
                case ReaperSkill.灵魂切割:
                    potency = 520; _slices++; _s = _s with { SliceCharges = Math.Max(0, _s.SliceCharges - 1) }; AddSoul(50); break;
                case ReaperSkill.虚无收割:
                case ReaperSkill.交错收割:
                    potency = _reaps++ == 0 ? 580 : 640; _s = _s with { Lemure = _s.Lemure - 1, Void = _s.Void + 1 }; break;
                case ReaperSkill.夜游魂切割:
                    potency = 280; _s = _s with { Void = _s.Void - 2 }; break;
                case ReaperSkill.祭性:
                    potency = 700; _s = _s with { Oblatio = 0 }; break;
                case ReaperSkill.团契:
                    potency = 1100; _coms++; if (_s.CircleLeft > 0) _buffComs++;
                    _s = _s with { Enshrouded = 0, Lemure = 0, Void = 0, Oblatio = 0, Perfectio = _s.Occulta > 0 ? 30 : 0, Occulta = 0, CastingCommunio = false }; break;
                case ReaperSkill.大丰收:
                    potency = 720 + Math.Clamp(_s.SacrificeStacks - 1, 0, 7) * 40;
                    _s = _s with { SacrificeStacks = 0, SacrificeLeft = 0, FreeEnshroud = 30, Occulta = 30 }; break;
                case ReaperSkill.完人:
                    potency = 1300; _perfects++; if (_s.CircleLeft > 0) _buffPerfects++;
                    _s = _s with { Perfectio = 0 }; break;
                case ReaperSkill.收获月:
                    potency = 800; _s = _s with { Soulsow = false }; AddSoul(10); break;
                case ReaperSkill.勾刃:
                    potency = 300; AddSoul(10);
                    // 已有瞬发只计一次，后续不假定再次位移获得强化。
                    if (_s.HarpeCast <= 0) _s = _s with { HarpeCast = 1.3f };
                    break;
                default:
                    potency = id == ReaperSkill.切割 ? 420 : id == ReaperSkill.增盈切割 ? 500 : 600;
                    _s = _s with { ComboNext = id == ReaperSkill.切割 ? ReaperSkill.增盈切割 : id == ReaperSkill.增盈切割 ? ReaperSkill.地狱切割 : 0,
                        ComboLeft = id == ReaperSkill.地狱切割 ? 0 : 30 }; AddSoul(10); break;
            }
            _damage += potency * multiplier;
            if (inBuff)
            {
                _buffDamage += potency * multiplier;
                if (id == ReaperSkill.完人) _buffPerfectioAt = _time;
                if (id == ReaperSkill.团契) _buffCommunioAt = _time;
            }
            // 越早落地越有价值。溢出的资源按保守机会成本扣分，不把清空量谱当奖励。
            if (_dump) _dumpScore += ReaperDumpPlanner.ScoreDamage(potency * multiplier,
                _lostSoul - oldSoulLoss, _lostShroud - oldShroudLoss, _time);
        }
    }
}
