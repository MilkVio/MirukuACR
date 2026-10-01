using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Planning;

// 在同一条可执行的产电/野火时序上比较所有合法召唤，不替机器人另造一套超荷循环。
// 状态只有电量和机器人退场时点；允许平峰多次召唤，也允许在工具后以80/90电进团辅。
internal static class MachinistBatteryPlanner
{
    internal readonly record struct Event(float At, int Gain, bool Summon);
    private readonly record struct Key(int Battery, int Ready);
    private readonly record struct Path(float Value, float FirstAt, int FirstBattery, int Lost,
        int BurstBattery = 0, float BurstCoverage = 0);
    internal readonly record struct Advice(bool Summon, float At, int Battery, int Lost, string Reason)
    {
        internal float Value { get; init; }
        internal float WildfireAt { get; init; }
        internal int BurstBattery { get; init; }
    }

    internal static Advice Choose(MachinistState initial)
    {
        var normal = Plan(initial, 0);
        if (!CanDelayExcavator(initial)) return normal;
        for (var combos = 1; combos <= 2; combos++)
        {
            var delayed = Plan(initial, combos);
            if (delayed.Value > normal.Value + .01f && delayed.WildfireAt <= normal.WildfireAt + .05f) normal = delayed;
        }
        return normal;
    }

    internal static MachinistChoice SelectGcd(MachinistState initial, MachinistChoice normal)
    {
        MachinistModel.Advance(ref initial, initial.GcdLeft);
        if (!CanDelayExcavator(initial)) return normal;
        var now = Plan(initial, 0);
        for (var combos = 1; combos <= 2; combos++)
        {
            var delayed = Plan(initial, combos);
            if (delayed.Value > now.Value + .01f && delayed.WildfireAt <= now.WildfireAt + .05f)
                return new(MachinistRules.Combo(initial), "电量比较：先完成连击，在飞轮20电入账前安排平峰召唤");
        }
        return normal;
    }

    private static Advice Plan(MachinistState initial, int delayCombos)
    {
        var anchor = MachinistRules.DamageBurstAt(initial);
        if (anchor <= 0) anchor += 120;
        var horizon = float.IsFinite(anchor) ? Math.Clamp(anchor + 20 + initial.Gcd, 30, 145) : 35;
        var events = Timeline(initial, horizon, delayCombos, out var complete, out var heatLost, out var wildfireAt);
        if (!complete) return new(false, 0, 0, 0, "电量前瞻未完成，等待实况重算") { Value = float.NegativeInfinity };
        var result = Allocate(initial, events);
        return result with { Value = result.Value - heatLost * 12 / (MachinistQueen.FullPotency / 50), WildfireAt = wildfireAt };
    }

    // 可复用的电量分配。热量候选也提交自己的产电/插槽时序，比较真实机器人覆盖。
    internal static Advice Allocate(MachinistState initial, List<Event> events)
    {
        events = events.OrderBy(e => e.At).Distinct().ToList();
        var nodes = new Dictionary<Key, Path>();
        int Ready(float at) => events.FindIndex(e => e.At + .001f >= at) is var index && index >= 0 ? index : events.Count;
        nodes[new(initial.Battery, Ready(Math.Max(initial.QueenLeft, initial.QueenCd)))] =
            new(0, float.PositiveInfinity, 0, 0);
        static bool Better(Path a, Path b) => a.Value > b.Value + .001f
            || Math.Abs(a.Value - b.Value) <= .001f && (a.Lost < b.Lost
                || a.Lost == b.Lost && a.FirstAt < b.FirstAt - .001f);
        var lastBonus = float.NaN;
        var (burstStart, burstEnd) = MachinistRules.QueenWindow(initial);
        for (var i = 0; i < events.Count; i++)
        {
            var e = events[i];
            var bonus = e.Summon ? MachinistQueen.Damage(initial, 50, e.At, float.PositiveInfinity)
                / MachinistQueen.FullPotency - 1 : 0;
            // 同电量、同覆盖且没有机器人刚退场时，较晚召唤被较早召唤严格支配。
            // 保留20ms时序验算，但不用为这些等价位置反复展开电量状态。
            if (e.Summon && Math.Abs(bonus - lastBonus) < .00001f
                && !nodes.Keys.Any(k => k.Ready != 0 && k.Ready <= i)) continue;
            lastBonus = e.Gain > 0 ? float.NaN : bonus;
            var next = new Dictionary<Key, Path>();
            void Keep(Key key, Path path)
            {
                if (key.Ready <= i) key = key with { Ready = 0 };
                if (!next.TryGetValue(key, out var old) || Better(path, old)) next[key] = path;
            }
            var ready = e.Summon ? Ready(e.At + MachinistQueen.Duration) : 0;
            // 基础威力对电量完全线性；未花电量仍值同样的基础威力，只比较覆盖加成和溢出损失。
            foreach (var (key, path) in nodes)
            {
                var lost = Math.Max(0, key.Battery + e.Gain - 100);
                Keep(new(Math.Min(100, key.Battery + e.Gain), key.Ready),
                    path with { Value = path.Value - lost, Lost = path.Lost + lost });
                if (!e.Summon || key.Battery < 50 || key.Ready > i) continue;
                var coverage = key.Battery * MachinistQueen.Coverage(e.At, burstEnd, burstStart);
                var bestBurst = coverage > path.BurstCoverage;
                Keep(new(0, ready), path with { Value = path.Value + key.Battery * bonus,
                    FirstAt = float.IsFinite(path.FirstAt) ? path.FirstAt : e.At,
                    FirstBattery = float.IsFinite(path.FirstAt) ? path.FirstBattery : key.Battery,
                    BurstBattery = bestBurst ? key.Battery : path.BurstBattery,
                    BurstCoverage = Math.Max(coverage, path.BurstCoverage) });
            }
            nodes = next;
        }
        var best = new Path(float.NegativeInfinity, float.PositiveInfinity, 0, 0);
        foreach (var path in nodes.Values) if (Better(path, best)) best = path;
        var use = best.FirstAt < initial.GcdLeft - MachinistWildfire.SubmitTail(initial);
        var reason = float.IsFinite(best.FirstAt)
            ? $"电量滚动分配：{best.FirstAt:F1}s后{best.FirstBattery}电召唤，预计溢电{best.Lost}"
            : "当前时序没有可兑现的召唤，按实况重算";
        return new(use, best.FirstAt, best.FirstBattery, best.Lost, reason)
        { Value = best.Value, BurstBattery = best.BurstBattery };
    }

    private static List<Event> Timeline(MachinistState initial, float horizon, int delayCombos, out bool complete,
        out int heatLost, out float wildfireAt)
    {
        heatLost = 0;
        wildfireAt = float.PositiveInfinity;
        var events = new List<Event>();
        var s = initial;
        var anchor = MachinistRules.DamageBurstAt(initial);
        if (anchor <= 0) anchor += 120;
        var time = 0f;
        for (var steps = 0; steps < 4000 && time < horizon - .001f; steps++)
        {
            if (s.GcdLeft <= .001f && s.Lock <= .001f)
            {
                var action = MachinistRules.Gcd(s, planBattery: false).Action;
                if (delayCombos > 0 && action == MCHSkill.掘地飞轮 && CanDelayExcavator(s))
                {
                    action = MachinistRules.Combo(s);
                    if (MachinistModel.BatteryGain(action) > 0) delayCombos--;
                }
                if (action == MCHSkill.掘地飞轮) delayCombos = 0;
                if (MachinistModel.BatteryGain(action) is var gain && gain > 0) events.Add(new(time, gain, false));
                heatLost += Math.Max(0, s.Heat + MachinistModel.HeatGain(action) - 100);
                MachinistModel.Apply(ref s, action);
                continue;
            }
            if (s.Lock <= .001f && MachinistRules.Slots(s) > 0)
            {
                var heat = MachinistProjection.CanSpendHeat(s, out _, out var revisitTiming);
                // 只提供普通规则真正会让给机器人的位置；野火、枪管及已承诺超荷仍排在前面。
                var available = s with { Battery = 100, QueenLeft = 0, QueenCd = 0 };
                if (MachinistRules.Off(available, true, heat, planBattery: false).Action == MCHSkill.后式自走人偶)
                {
                    events.Add(new(time, 0, true));
                    // 团辅前可在本G后段召唤，让第一击进入增益；等待留在ACR内，不占宿主队列。
                    var alignment = anchor - MachinistQueen.FirstHit + .03f;
                    if (alignment > time + .001f && alignment < time + s.GcdLeft - MachinistWildfire.SubmitTail(s) - .03f)
                        events.Add(new(alignment, 0, true));
                }
                var choice = MachinistRules.Off(s, false, heat, planBattery: false);
                if (choice.Action != 0)
                {
                    var wait = Math.Max(0, choice.Delay - (s.GcdTotal - s.GcdLeft));
                    MachinistModel.Advance(ref s, wait); time += wait;
                    if (choice.Action == MCHSkill.野火) wildfireAt = Math.Min(wildfireAt, time);
                    if (time < horizon) MachinistModel.Apply(ref s, choice.Action, choice.RecoveryWildfire);
                    continue;
                }
                // 没有动作不能虚构一次插入。临近野火的热量许可可能在本G内改变，
                // 与实际滚动求解一样重新验算，而不是跳过整G、多预测一整套产电连击。
                if (revisitTiming)
                {
                    var tick = Math.Min(.02f, Math.Min(s.GcdLeft, horizon - time));
                    MachinistModel.Advance(ref s, tick); time += tick;
                    continue;
                }
            }
            var dt = s.Lock > .001f ? s.GcdLeft > .001f ? Math.Min(s.Lock, s.GcdLeft) : s.Lock : s.GcdLeft;
            if (s.WildfireQt && s.WildfireCd > .001f) dt = Math.Min(dt, s.WildfireCd);
            if (s.BarrelQt && s.BarrelCd > .001f) dt = Math.Min(dt, s.BarrelCd);
            dt = Math.Min(Math.Max(.001f, dt), horizon - time);
            MachinistModel.Advance(ref s, dt); time += dt;
        }
        complete = time >= horizon - .001f;
        return events.OrderBy(e => e.At).Distinct().ToList();
    }

    private static bool CanDelayExcavator(MachinistState s)
    {
        if (s.Level != 100 || !s.Alive || !s.HasTarget || !s.QueenQt || s.WindowActive || s.DumpQt || s.SawFirst || s.AoeQt && s.Targets >= 3
            || s.Heated || s.Reassemble > 0 || s.DamageWindow > 0 || s.Excavator <= 0
            || s.FullMetalPending || s.FreeHypercharge > 0 || s.WildfireLeft > 0) return false;
        var anchor = MachinistRules.BurstAt(s);
        if (anchor is <= 20 or > 100) return false;
        var gcds = s.ComboNext == MCHSkill.热狙击弹3 ? 1 : s.ComboNext == MCHSkill.热独头弹2 ? 2 : 3;
        return gcds > 0 && s.Excavator > (gcds + 1) * s.Gcd + MachinistRules.EffectMargin
            && s.QueenLeft <= (gcds - 1) * s.Gcd + s.WeaveLock && s.QueenCd <= (gcds - 1) * s.Gcd + s.WeaveLock;
    }
}
