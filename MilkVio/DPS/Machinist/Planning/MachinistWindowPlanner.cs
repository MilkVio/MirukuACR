using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Planning;

// 有限候选的滚动收益比较。每条路线同时验证两种量谱；不以花光资源作为评分。
// 长阶段采用600秒前瞻，明确区分前瞻终点与真正资源交付点，不虚报“期末达标”。
internal static class MachinistWindowPlanner
{
    internal const float Lookahead = 600;
    internal readonly record struct Result(MachinistState End, float Damage, int HeatLost, int BatteryLost,
        int HeatMissing, int BatteryMissing, int EndMissing, float WildfireAt, bool Complete, bool Terminal)
    {
        internal int Worst => Math.Max(HeatMissing, BatteryMissing);
        internal int Missing => HeatMissing + BatteryMissing;
    }
    internal readonly record struct Advice(MachinistChoice Choice, Result Result, string Reason);
    // 热：0保留、1围绕120、2尽早；电：0保留、1围绕120、2尽早、3满电。
    private readonly record struct Policy(int Heat, int Queen, bool Resources);
    private static readonly Policy[] Policies = MakePolicies();

    private static Policy[] MakePolicies()
    {
        var policies = new List<Policy>();
        for (var heat = 0; heat <= 2; heat++)
        for (var queen = 0; queen <= 3; queen++)
        {
            policies.Add(new(heat, queen, false));
            policies.Add(new(heat, queen, true));
        }
        return policies.ToArray();
    }

    internal static Advice Choose(MachinistState initial, bool off, uint reserved = 0, uint held = 0)
    {
        var candidates = new List<MachinistChoice>();
        void Add(MachinistChoice choice)
        {
            if (!candidates.Any(c => c.Action == choice.Action && Math.Abs(c.Delay - choice.Delay) < .001f))
                candidates.Add(choice);
        }
        var s = initial;
        if (off)
        {
            foreach (var policy in Policies) Add(NextOff(s, policy));
            Add(new(0, "保留本G剩余插槽"));
        }
        else
        {
            MachinistModel.Advance(ref s, s.GcdLeft);
            Add(MachinistRules.Gcd(s, reserved, held));
            // 已交出的整备绑定、过热及野火第一个普通G不能被候选搜索拆开。
            if (!s.Heated && !(s.Reassemble > 0 && MachinistRules.ToolReady(s, reserved))
                && !(s.WildfireLeft > 0 && s.WildfireLead && !s.LeadGcdDone))
            {
                Add(new(MachinistRules.Combo(s), "连击与资源交付"));
                foreach (var id in new[] { MCHSkill.空气锚, MCHSkill.回转飞锯, MCHSkill.掘地飞轮, MCHSkill.钻头 })
                    if (id != held && MachinistRules.ToolReady(s, id)) Add(new(id, "工具收益比较"));
                if (s.FullMetal > MachinistRules.EffectMargin && s.Reassemble <= 0)
                    Add(new(MCHSkill.全金属爆发, "全金属收益比较"));
            }
            if (s.SawFirst && MachinistRules.ToolReady(s, MCHSkill.回转飞锯) && held != MCHSkill.回转飞锯
                && s.Excavator <= 0 && !s.Heated && reserved == 0)
                candidates.RemoveAll(c => c.Action != MCHSkill.回转飞锯);
        }

        Advice best = default;
        var found = false;
        var bestPriority = 0;
        var follow = off && initial.FastWildfireActive ? MachinistRules.Off(initial, false, false) : default;
        var followAction = follow.Action is MCHSkill.超荷 or MCHSkill.枪管加热 ? follow.Action : 0;
        var fastGcd = !off && (s.FastWildfireActive || s.FastBurst && s.WildfireQt && s.WildfireLeft <= 0
            && s.WildfireCd < s.Gcd - MachinistWildfire.SubmitTail(s)
            && s.WindowLeft >= Math.Max(s.WildfireCd, s.WeaveLock) + 10 + MachinistRules.EffectMargin);
        // 本窗口能结算的野火按最早可行时点安排，期末双资源可达性优先于这条偏好。
        var punctual = Punctual(initial);
        foreach (var candidate in candidates)
        {
            var result = Project(initial, candidate, off);
            if (!result.Complete) continue;
            // 资源缺口与最早野火时点仍在前；快速路线再争取本轮命中和已可衔接的超荷。
            var priority = fastGcd ? MachinistFastBurst.AfterGcd(initial, candidate.Action).Hits
                : followAction != 0 && candidate.Action == followAction ? 1 : 0;
            if (!found || Better(result, best.Result, punctual, priority - bestPriority))
            {
                best = new(candidate, result, "");
                bestPriority = priority;
                found = true;
            }
        }
        if (!found) return new(new(0, "窗口预测未完成"), default, "窗口预测未完成，等待重算");
        var r = best.Result;
        var deadline = initial.ReserveEarly ? initial.ReserveLeft : initial.WindowLeft;
        var target = r.Terminal || deadline <= Lookahead
            ? r.Missing > 0 ? $"目标暂不可达：缺热{r.HeatMissing}/电{r.BatteryMissing}"
                : $"双资源达标，预计留热{r.End.Heat}/电{r.End.Battery}"
            : $"滚动前瞻{Lookahead:F0}s，资源交付还有{deadline:F1}s";
        var reason = $"窗口收益比较：{target}；预计威力{r.Damage:F0}，溢热{r.HeatLost}/电{r.BatteryLost}；{best.Choice.Reason}";
        return best with { Reason = reason, Choice = best.Choice with { Reason = reason } };
    }

    internal static Result Project(MachinistState initial, MachinistChoice first, bool off)
    {
        Result best = default;
        var found = false;
        foreach (var policy in Policies)
        {
            var result = Run(initial, first, off, policy);
            var punctual = Punctual(initial);
            if (result.Complete && (!found || Better(result, best, punctual))) { best = result; found = true; }
        }
        return best;
    }

    private static bool Better(Result a, Result b, bool punctual, int priority = 0)
    {
        if (a.Worst != b.Worst) return a.Worst < b.Worst;
        if (a.Missing != b.Missing) return a.Missing < b.Missing;
        if (a.EndMissing != b.EndMissing) return a.EndMissing < b.EndMissing;
        if (punctual && Math.Abs(a.WildfireAt - b.WildfireAt) > .05f) return a.WildfireAt < b.WildfireAt;
        if (priority != 0) return priority > 0;
        if (Math.Abs(a.Damage - b.Damage) > .5f) return a.Damage > b.Damage;
        if (a.HeatLost + a.BatteryLost != b.HeatLost + b.BatteryLost)
            return a.HeatLost + a.BatteryLost < b.HeatLost + b.BatteryLost;
        return false;
    }

    // 不能等野火已经转好才保护时点；前几G的超荷或机器人也可能推迟它。
    // 资源交付缺口仍排在前面，无法在窗口结算的野火不占用这个优先级。
    private static bool Punctual(MachinistState s) => s.WildfireQt
        && s.WildfireCd + 10 + MachinistRules.EffectMargin <= Math.Min(Lookahead, s.WindowLeft);

    private static MachinistChoice NextGcd(MachinistState s, Policy policy)
    {
        var normal = MachinistRules.Gcd(s);
        if (!policy.Resources || s.Heated || s.Reassemble > 0 || s.SawFirst
            || s.WildfireLeft > 0 && s.WildfireLead && !s.LeadGcdDone) return normal;
        var left = s.ReserveEarly ? s.ReserveLeft : s.WindowLeft;
        // 两种保留路线都参与比较；只改变临近交付时的资源GCD，避免长期丢工具。
        if (left > 15 || s.GoalHeat == 0 && s.GoalBattery == 0) return normal;
        var combo = MachinistRules.Combo(s);
        if (s.Heat < s.GoalHeat && MachinistModel.HeatGain(combo) > 0)
            return new(combo, "先补交付热量");
        if (s.Battery < s.GoalBattery)
        {
            if (MachinistModel.BatteryGain(normal.Action) > 0) return normal;
            if (MachinistModel.BatteryGain(combo) > 0) return new(combo, "先补交付电量");
            foreach (var tool in new[] { MCHSkill.空气锚, MCHSkill.掘地飞轮, MCHSkill.回转飞锯 })
                if (MachinistRules.ToolReady(s, tool)) return new(tool, "产电工具优先");
        }
        return normal;
    }

    private static MachinistChoice NextOff(MachinistState s, Policy policy)
    {
        // 普通野火的完整路线必须兑现；快速野火允许在资源保留优先时放弃超荷。
        if (!s.FastWildfireActive && s.WildfireLeft > 0 && s.HyperchargeQt && s.CanHypercharge && (!s.WildfireLead || s.LeadGcdDone))
            return MachinistRules.Off(s, false, false, true, windowHeat: true);
        // 保留一条不新增资源Buff的可行见证。枪管新增全金属同样会占用产量谱的GCD。
        if (policy.Heat == 0 && policy.Queen == 0 && policy.Resources && (s.GoalHeat > 0 || s.GoalBattery > 0))
        {
            var conserve = s with { BarrelQt = false, ReassembleQt = false, WildfireQt = false };
            return MachinistRules.Off(conserve, false, false, false);
        }
        var (start, end) = MachinistRules.QueenWindow(s);
        var next = MachinistRules.Gcd(s).Action;
        var queen = policy.Queen switch
        {
            1 => start <= MachinistQueen.FirstHit && end > MachinistQueen.FirstHit
                || s.Battery + MachinistModel.BatteryGain(next) > 100
                || s.WindowActive && s.WindowLeft <= MachinistQueen.Duration + s.Gcd,
            2 => true,
            3 => s.Battery == 100,
            _ => false
        };
        queen &= !s.WindowActive || s.WindowLeft > MachinistQueen.FirstHit + MachinistRules.EffectMargin;
        var heat = policy.Heat == 2 || policy.Heat == 1 && (s.Heat >= 95
            || s.FreeHypercharge > 0 && s.FreeHypercharge < 10 || MachinistRules.BurstLeft(s) > 0
            || s.WindowActive && s.WindowLeft <= 20);
        // 提前花热量不能让下一次仍能结算的野火失去最后一份资源。
        if (s.WildfireQt && s.WildfireCd <= 15 && s.WildfireCd + 10 + MachinistRules.EffectMargin <= s.WindowLeft
            && s.FreeHypercharge <= 0 && s.BarrelCd > s.WildfireCd && s.Heat < 100) heat = false;
        if (heat && s.WildfireQt && s.WildfireCd is > 0 and <= 30 && Punctual(s))
            heat = MachinistProjection.CanSpendHeat(s with { WindowActive = false, GoalHeat = 0, GoalBattery = 0 }, out _);
        return MachinistRules.Off(s, queen, heat, policy.Heat != 0, windowHeat: true);
    }

    private static Result Run(MachinistState initial, MachinistChoice first, bool off, Policy policy)
    {
        var s = initial;
        // 后窗口只比较眼前可兑现的动作，不假定容错时间必定可以输出。
        var horizon = Math.Min(Lookahead, initial.WindowLeft > 0 ? initial.WindowLeft : Math.Min(initial.WindowLimit, Math.Max(2, initial.Gcd)));
        var terminal = initial.WindowActive && initial.WindowLeft <= Lookahead;
        var reserveAt = initial.ReserveEarly ? initial.ReserveLeft : initial.WindowLeft;
        var checkReserve = reserveAt <= horizon + .001f;
        var time = 0f;
        var damage = 0f;
        var heatLost = 0; var batteryLost = 0; var heatMissing = 0; var batteryMissing = 0;
        var wildfireAt = float.PositiveInfinity;
        var wildfireMultiplier = MachinistDamage.Multiplier(initial, 0);
        void Reserve()
        {
            if (!checkReserve || time + .001f < reserveAt) return;
            heatMissing = Math.Max(heatMissing, Math.Max(0, initial.GoalHeat - s.Heat));
            batteryMissing = Math.Max(batteryMissing, Math.Max(0, initial.GoalBattery - s.Battery));
        }
        void Advance(float dt)
        {
            dt = Math.Min(Math.Max(0, dt), horizon - time);
            if (s.WildfireLeft > 0 && s.WildfireLeft <= dt && time + s.WildfireLeft + MachinistRules.EffectMargin <= horizon)
                damage += 240 * s.WildfireHits * wildfireMultiplier;
            if (time < reserveAt && time + dt >= reserveAt)
            {
                heatMissing = Math.Max(heatMissing, Math.Max(0, initial.GoalHeat - s.Heat));
                batteryMissing = Math.Max(batteryMissing, Math.Max(0, initial.GoalBattery - s.Battery));
            }
            MachinistModel.Advance(ref s, dt); time += dt;
            Reserve();
        }
        void Apply(uint action)
        {
            // 资源随GCD效果到达，不能把交付点之后才生效的收入提前借进来。
            if (checkReserve && time < reserveAt && time + MachinistRules.EffectMargin > reserveAt
                && MachinistRules.IsWeaponskill(action))
            {
                heatMissing = Math.Max(heatMissing, Math.Max(0, initial.GoalHeat - s.Heat));
                batteryMissing = Math.Max(batteryMissing, Math.Max(0, initial.GoalBattery - s.Battery));
            }
            if (action == MCHSkill.后式自走人偶)
                damage += MachinistQueen.Damage(initial, s.Battery, time, initial.WindowActive ? horizon : float.PositiveInfinity);
            else if (action == MCHSkill.野火)
            {
                wildfireAt = Math.Min(wildfireAt, time);
                wildfireMultiplier = MachinistDamage.Multiplier(initial, time);
            }
            else if (time + MachinistRules.EffectMargin <= horizon)
                damage += MachinistDamage.Action(s, action) * MachinistDamage.Multiplier(initial, time);
            heatLost += Math.Max(0, s.Heat + MachinistModel.HeatGain(action) - 100);
            batteryLost += Math.Max(0, s.Battery + MachinistModel.BatteryGain(action) - 100);
            MachinistModel.Apply(ref s, action);
            Reserve();
        }
        Reserve();
        if (!off) Advance(s.GcdLeft);
        else if (first.Action != 0) Advance(Math.Max(s.Lock, first.Delay - (s.GcdTotal - s.GcdLeft)));
        if (first.Action != 0 && time + MachinistRules.EffectMargin >= horizon)
            return new(s, 0, 0, 0, heatMissing, batteryMissing, 0, wildfireAt, false, terminal);
        if (time + MachinistRules.EffectMargin < horizon)
        {
            if (first.Action != 0) Apply(first.Action);
            else if (off) s.Weaves = s.WeaveLimit; // 等待候选放弃本G剩余插槽，必须推进。
        }
        var steps = 0;
        for (; time < horizon - .001f && steps < 10000; steps++)
        {
            if (s.GcdLeft <= .001f)
            {
                if (s.Lock > .001f) { Advance(s.Lock); continue; }
                if (time + MachinistRules.EffectMargin >= horizon) { Advance(horizon - time); break; }
                Apply(NextGcd(s, policy).Action);
                continue;
            }
            if (s.Lock <= .001f && MachinistRules.Slots(s) > 0 && s.GcdLeft > MachinistWildfire.SubmitTail(s))
            {
                var choice = NextOff(s, policy);
                if (choice.Action != 0)
                {
                    Advance(Math.Max(0, choice.Delay - (s.GcdTotal - s.GcdLeft)));
                    if (time + MachinistRules.EffectMargin < horizon) { Apply(choice.Action); continue; }
                }
            }
            var dt = s.Lock > .001f ? Math.Min(s.Lock, s.GcdLeft) : s.GcdLeft;
            if (s.WildfireQt && s.WildfireCd > .001f) dt = Math.Min(dt, s.WildfireCd);
            if (s.BarrelQt && s.BarrelCd > .001f) dt = Math.Min(dt, s.BarrelCd);
            Advance(Math.Max(.001f, dt));
        }
        Reserve();
        // 滚动前瞻不是阶段末尾，剩余量谱及已召机器人仍有后续价值。
        if (!terminal) damage += s.Battery * MachinistQueen.FullPotency / 50 + s.Heat * 12;
        var endMissing = checkReserve ? Math.Max(0, initial.GoalHeat - s.Heat) + Math.Max(0, initial.GoalBattery - s.Battery) : 0;
        return new(s, damage, heatLost, batteryLost, heatMissing, batteryMissing, endMissing, wildfireAt,
            time >= horizon - .001f && steps < 10000, terminal);
    }
}
