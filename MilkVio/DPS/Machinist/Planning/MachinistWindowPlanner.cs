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
        internal float DrillLost { get; init; }
        internal float ToolDelay { get; init; }
        internal int Tools { get; init; }
        internal int HeatCycles { get; init; }
        internal float HyperchargeAt { get; init; }
        internal float ResourceCost { get; init; }
        internal float Score => Damage - ResourceCost;
        internal bool HeatWasteAvoided { get; init; }
        internal int Missing => HeatMissing + BatteryMissing;
    }
    internal readonly record struct Advice(MachinistChoice Choice, Result Result, string Reason);
    // 热：0保留、1围绕120、2尽早；电：0保留、1围绕120、2尽早、3满电。
    private readonly record struct Policy(int Heat, int Queen, bool Resources, bool SawFirst);
    private readonly record struct Route(MachinistChoice Choice, Result Result, int Priority = 0, int ToolPriority = 0);
    private static readonly Policy[] Policies = MakePolicies();
    private static readonly Policy[] SpendPolicies = Policies.Where(p => !p.Resources).ToArray();
    private static Policy[] PoliciesFor(MachinistState s) => s.GoalHeat == 0 && s.GoalBattery == 0 ? SpendPolicies : Policies;

    private static Policy[] MakePolicies()
    {
        var policies = new List<Policy>();
        for (var heat = 0; heat <= 2; heat++)
        for (var queen = 0; queen <= 3; queen++)
        foreach (var saw in new[] { false, true })
        {
            policies.Add(new(heat, queen, false, saw));
            policies.Add(new(heat, queen, true, saw));
        }
        return policies.ToArray();
    }

    internal static Advice Choose(MachinistState initial, bool off, uint reserved = 0, uint held = 0)
    {
        var candidates = new List<MachinistChoice>();
        void Add(MachinistChoice choice)
        {
            if (!candidates.Any(c => c.Action == choice.Action && c.Tool == choice.Tool
                && c.RecoveryWildfire == choice.RecoveryWildfire && Math.Abs(c.Delay - choice.Delay) < .001f))
                candidates.Add(choice);
        }
        var s = initial;
        if (off)
        {
            foreach (var policy in PoliciesFor(s)) Add(NextOff(s, policy));
            Add(new(0, "保留本G剩余插槽"));
        }
        else
        {
            MachinistModel.Advance(ref s, s.GcdLeft);
            var normal = Gcd(s, reserved, held);
            Add(normal);
            // 冷却保护限制候选种类，不锁死锚/钻/锯的顺序。后续同样比较锚、锯路线，
            // 并在下一G钻头将满层时先腾充能，避免仅换首G却仍让锚抢掉次G。
            if (!ToolOrderLocked(s, normal, reserved, held))
            {
                if (!ToolsDue(s)) Add(new(MachinistRules.Combo(s), "连击与资源交付"));
                foreach (var id in new[] { MCHSkill.空气锚, MCHSkill.回转飞锯, MCHSkill.掘地飞轮, MCHSkill.钻头 })
                    if (id != held && MachinistRules.ToolReady(s, id)
                        && !(id == MCHSkill.回转飞锯 && MachinistRules.ToolReady(s, MCHSkill.掘地飞轮)))
                        Add(new(id, "工具收益比较"));
                if (!ToolsDue(s) && s.FullMetalPending && s.FullMetal > MachinistRules.EffectMargin && s.Reassemble <= 0)
                    Add(new(MCHSkill.全金属爆发, "全金属收益比较"));
            }
            if (s.SawFirst && MachinistRules.ToolReady(s, MCHSkill.回转飞锯) && held != MCHSkill.回转飞锯
                && s.Excavator <= 0 && !s.Heated && reserved == 0)
                candidates.RemoveAll(c => c.Action != MCHSkill.回转飞锯);
        }

        var routes = new List<Route>();
        var follow = off && initial.FastWildfireActive ? MachinistRules.Off(initial, false, false) : default;
        var followAction = follow.Action is MCHSkill.超荷 or MCHSkill.枪管加热 ? follow.Action : 0;
        var fastGcd = !off && (s.FastWildfireActive || s.FastBurst && s.WildfireQt && s.WildfireLeft <= 0
            && s.WildfireCd < s.Gcd - MachinistWildfire.SubmitTail(s)
            && s.WindowLeft >= Math.Max(s.WildfireCd, s.WeaveLock) + 10 + MachinistRules.EffectMargin);
        var normalTool = !off && ToolsDue(s) ? Gcd(s, reserved, held).Action : 0;
        // 资源交付不能推迟本窗口能结算的野火。
        foreach (var candidate in candidates)
        {
            var result = Project(initial, candidate, off);
            if (!result.Complete) continue;
            var priority = fastGcd ? MachinistFastBurst.AfterGcd(initial, candidate.Action).Hits
                : followAction != 0 && candidate.Action == followAction ? 1 : 0;
            var toolPriority = normalTool != 0 && candidate.Action == normalTool ? 1 : 0;
            routes.Add(new(candidate, result, priority, toolPriority));
        }
        if (routes.Count == 0) return new(new(0, "窗口预测未完成"), default, "窗口预测未完成，等待重算");
        var best = Select(routes, initial, off);
        var r = best.Result;
        var target = r.Terminal
            ? r.Missing > 0 ? $"尽力交付：预计留热{r.End.Heat}/电{r.End.Battery}，缺热{r.HeatMissing}/电{r.BatteryMissing}"
                : $"双资源达标，预计留热{r.End.Heat}/电{r.End.Battery}"
            : $"滚动前瞻{Lookahead:F0}s，资源交付还有{initial.WindowLeft:F1}s";
        var heatAt = float.IsFinite(r.HyperchargeAt) ? $"{r.HyperchargeAt:F2}s" : "无";
        var guard = r.HeatWasteAvoided ? "，已排除为交付少打一轮且多溢50热的路线" : "";
        var reason = $"窗口收益比较：{target}；预计工具{r.Tools}次、威力{r.Damage:F0}，资源代价{r.ResourceCost:F0}，净评分{r.Score:F0}；过热{r.HeatCycles}组，下一超荷{heatAt}，溢热{r.HeatLost}/电{r.BatteryLost}，钻头满层损失{r.DrillLost:F2}s{guard}；{best.Choice.Reason}";
        return new(best.Choice with { Reason = reason }, r, reason);
    }

    internal static Result Project(MachinistState initial, MachinistChoice first, bool off)
    {
        var routes = new List<Route>();
        foreach (var policy in PoliciesFor(initial))
        {
            var result = Run(initial, first, off, policy);
            var priority = policy.SawFirst ? 0 : 1;
            if (result.Complete) routes.Add(new(first, result, ToolPriority: priority));
        }
        return routes.Count == 0 ? default : Select(routes, initial, off).Result;
    }

    private static Route Select(List<Route> routes, MachinistState initial, bool off)
    {
        // 先在整组候选中筛硬约束，再比较收益。不能把“多溢50热”写成两两比较：
        // 该关系不传递，会令最终动作随候选枚举顺序改变。
        void KeepMin(Func<Route, float> value, float tolerance = 0)
        {
            var min = routes.Min(value);
            routes.RemoveAll(r => value(r) > min + tolerance);
        }
        // GCD能在超荷前消除满层损失时必须先处理，不能拿期末量谱覆盖它。
        // oGCD决定时当前G已经交出，不能再为修正那一G反复延后野火。
        if (!off) KeepMin(r => r.Result.DrillLost, .05f);
        if (Punctual(initial)) KeepMin(r => r.Result.WildfireAt, .05f);
        KeepMin(r => -r.Priority);
        if (off) KeepMin(r => r.Result.DrillLost, .05f);
        // 保留现有的工具兑现次数和稳定顺序保护，资源目标不能再排在它们前面。
        KeepMin(r => -r.Result.Tools);
        KeepMin(r => -r.ToolPriority);
        var avoided = false;
        if (initial.GoalHeat > 0 || initial.GoalBattery > 0)
        {
            var safe = routes.ToArray();
            avoided = routes.RemoveAll(a => a.Result.Terminal && safe.Any(b =>
                b.Result.Tools >= a.Result.Tools && b.Result.HeatCycles > a.Result.HeatCycles
                && a.Result.HeatLost >= b.Result.HeatLost + 50
                && b.Result.Damage > a.Result.Damage + .5f)) > 0;
        }
        KeepMin(r => -r.Result.Score, .5f);
        KeepMin(r => r.Result.ResourceCost, .5f);
        KeepMin(r => r.Result.HeatLost + r.Result.BatteryLost);
        KeepMin(r => r.Result.ToolDelay, .05f);
        var best = routes[0];
        return best with { Result = best.Result with { HeatWasteAvoided = best.Result.HeatWasteAvoided || avoided } };
    }

    // 不能等野火已经转好才保护时点；前几G的超荷或机器人也可能推迟它。
    // 无法在窗口结算的野火不占用这个优先级。
    private static bool Punctual(MachinistState s) => s.WildfireQt
        && s.WildfireCd + 10 + MachinistRules.EffectMargin <= Math.Min(Lookahead, s.WindowLeft);

    // 在下一个GCD时点检查：锚/锯就绪即安排；钻头本G不打就将满层时提前保护。
    // 同时就绪、旧飞轮、临期预备和已提交的整备/过热仍由基础排序逐G兑现。
    private static bool ToolsDue(MachinistState s) => MachinistRules.ToolReady(s, MCHSkill.空气锚)
        || MachinistRules.ToolReady(s, MCHSkill.回转飞锯)
        || MachinistRules.ToolReady(s, MCHSkill.钻头)
            && s.Drill >= 2 - s.Gcd / Math.Max(1, s.DrillRecast) - .0001f;

    private static bool ToolOrderLocked(MachinistState s, MachinistChoice normal, uint reserved = 0, uint held = 0) =>
        s.Heated || held != 0 || s.Reassemble > 0 && MachinistRules.ToolReady(s, reserved)
        || s.WildfireLeft > 0 && s.WildfireLead && !s.LeadGcdDone
        || normal.Action == MCHSkill.掘地飞轮 && s.Excavator <= s.Gcd + MachinistRules.EffectMargin
        || normal.Action == MCHSkill.全金属爆发 && s.FullMetal <= s.Gcd + MachinistRules.EffectMargin
        || s.SawFirst && MachinistRules.ToolReady(s, MCHSkill.回转飞锯);

    private static MachinistChoice Gcd(MachinistState s, uint reserved = 0, uint held = 0, bool sawFirst = false)
    {
        MachinistModel.Advance(ref s, s.GcdLeft);
        if (!ToolsDue(s)) return MachinistRules.Gcd(s, reserved, held);
        var normal = MachinistRules.NormalGcd(s, reserved, held);
        if (ToolOrderLocked(s, normal, reserved, held)) return normal;
        if (MachinistRules.ToolReady(s, MCHSkill.钻头)
            && s.Drill >= 2 - s.Gcd / Math.Max(1, s.DrillRecast) - .0001f)
            return new(MCHSkill.钻头, "工具排序：本G先腾钻头充能");
        if (sawFirst && MachinistRules.ToolReady(s, MCHSkill.回转飞锯) && !MachinistRules.ToolReady(s, MCHSkill.掘地飞轮))
            return new(MCHSkill.回转飞锯, "工具排序：比较飞锯后续轮次与飞轮兑现");
        return normal with { Reason = $"三大件冷却保护；{normal.Reason}" };
    }

    internal static bool HyperchargeAllowed(MachinistState s)
    {
        if (!s.CanHypercharge) return false;
        if (s.FastWildfireActive) return MachinistFastBurst.CanHypercharge(s);
        if (s.WildfireLeft > 0 && !s.WildfireRecovery)
            return s.WildfireHits < 6 && (!s.WildfireLead || s.LeadGcdDone);
        return MachinistHeatPlanner.Fits(s)
            || MachinistWildfire.TryPlan(s, out var plan, out _) && plan.Choice(s).Action == MCHSkill.超荷;
    }

    private static MachinistChoice NextGcd(MachinistState s, Policy policy, uint reserved = 0)
    {
        var normal = Gcd(s, reserved, sawFirst: policy.SawFirst);
        // 预测与当前选招使用同一硬约束，不能靠后续压工具虚报资源可达。
        if (ToolsDue(s) || !policy.Resources || s.Heated || s.Reassemble > 0 || s.SawFirst
            || s.WildfireLeft > 0 && s.WildfireLead && !s.LeadGcdDone) return normal;
        var left = s.WindowLeft;
        // 保留候选按缺口估计补资源所需G数。只在最后15秒才开始、或只允许已到第三段的
        // 连击产电，会漏掉本可提前续连击的见证（D3交付点前差10电）。普通路线仍参与比较。
        var heatGcds = Math.Max(0, (s.GoalHeat - s.Heat + 4) / 5);
        var batteryGcds = Math.Max(0, (s.GoalBattery - s.Battery + 9) / 10) * 3;
        if (batteryGcds > 0) batteryGcds -= s.ComboNext == MCHSkill.热狙击弹3 ? 2
            : s.ComboNext == MCHSkill.热独头弹2 ? 1 : 0;
        if (left > Math.Max(15, Math.Max(heatGcds, batteryGcds) * s.Gcd + MachinistRules.EffectMargin)
            || s.GoalHeat == 0 && s.GoalBattery == 0) return normal;
        var combo = MachinistRules.Combo(s);
        if (s.Heat < s.GoalHeat && MachinistModel.HeatGain(combo) > 0)
            return new(combo, "先补交付热量");
        if (s.Battery < s.GoalBattery)
        {
            if (MachinistModel.BatteryGain(normal.Action) > 0) return normal;
            if (MachinistModel.BatteryGain(combo) > 0) return new(combo, "先补交付电量");
            foreach (var tool in new[] { MCHSkill.空气锚, MCHSkill.掘地飞轮, MCHSkill.回转飞锯 })
                if (MachinistRules.ToolReady(s, tool)) return new(tool, "产电工具优先");
            return new(combo, "提前完成交付电量所需连击");
        }
        return normal;
    }

    private static MachinistChoice NextOff(MachinistState s, Policy policy)
    {
        // 普通野火的完整路线必须兑现；快速野火由各条合法候选比较收益。
        if (!s.FastWildfireActive && !s.WildfireRecovery && s.WildfireLeft > 0 && s.HyperchargeQt && s.CanHypercharge && (!s.WildfireLead || s.LeadGcdDone))
            return MachinistRules.Off(s, false, false, true, windowHeat: true);
        // 保留一条不新增资源Buff的可行见证。枪管新增全金属同样会占用产量谱的GCD。
        if (policy.Heat == 0 && policy.Queen == 0 && policy.Resources && (s.GoalHeat > 0 || s.GoalBattery > 0))
        {
            var conserve = s with { BarrelQt = false, ReassembleQt = false, WildfireQt = false };
            return MachinistRules.Off(conserve, false, false, false);
        }
        var (start, end) = MachinistRules.QueenWindow(s);
        var nextState = s;
        MachinistModel.Advance(ref nextState, nextState.GcdLeft);
        var next = NextGcd(nextState, policy).Action;
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
            heat = MachinistProjection.CanSpendWindowHeat(s);
        // 普通窗口超荷也必须放得进工具冷却空隙，不能借窗口模式绕过Fits压住三大件。
        var choice = MachinistRules.Off(s, queen, heat, policy.Heat != 0);
        if (choice.Action != MCHSkill.整备) return choice;
        return MachinistRules.IsTool(next) ? choice with { Tool = next }
            : MachinistRules.Off(s with { ReassembleQt = false }, queen, heat, policy.Heat != 0);
    }

    private static Result Run(MachinistState initial, MachinistChoice first, bool off, Policy policy)
    {
        var s = initial;
        // 后窗口只比较眼前可兑现的动作，不假定容错时间必定可以输出。
        var horizon = Math.Min(Lookahead, initial.WindowLeft > 0 ? initial.WindowLeft : Math.Min(initial.WindowLimit, Math.Max(2, initial.Gcd)));
        var terminal = initial.WindowActive && initial.WindowLeft <= Lookahead;
        var reserveAt = initial.WindowLeft;
        var checkReserve = reserveAt <= horizon + .001f;
        var time = 0f;
        var damage = 0f;
        var drillLost = 0f;
        var toolDelay = 0f;
        var tools = 0;
        var heatCycles = 0;
        var hyperchargeAt = float.PositiveInfinity;
        uint reserved = 0;
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
            var drill = Math.Max(0, dt - (2 - s.Drill) * s.DrillRecast);
            drillLost += drill;
            toolDelay += drill + Math.Max(0, dt - s.AnchorCd) + Math.Max(0, dt - s.SawCd);
            MachinistModel.Advance(ref s, dt); time += dt;
            Reserve();
        }
        void Apply(uint action, uint tool = 0, bool recoveryWildfire = false)
        {
            if (action == MCHSkill.超荷) hyperchargeAt = Math.Min(hyperchargeAt, time);
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
            {
                damage += MachinistDamage.Action(s, action) * MachinistDamage.Multiplier(initial, time);
                if (MachinistRules.IsTool(action)) tools++;
                if (MachinistRules.IsHeatShot(action) && s.OverheatStacks == 1) heatCycles++;
            }
            heatLost += Math.Max(0, s.Heat + MachinistModel.HeatGain(action) - 100);
            batteryLost += Math.Max(0, s.Battery + MachinistModel.BatteryGain(action) - 100);
            MachinistModel.Apply(ref s, action, recoveryWildfire);
            if (action == MCHSkill.整备) reserved = tool;
            else if (MachinistRules.IsWeaponskill(action)) reserved = 0;
            Reserve();
        }
        Reserve();
        if (!off) Advance(s.GcdLeft);
        else if (first.Action != 0) Advance(Math.Max(s.Lock, first.Delay - (s.GcdTotal - s.GcdLeft)));
        if (first.Action != 0 && time + MachinistRules.EffectMargin >= horizon)
            return new(s, 0, 0, 0, heatMissing, batteryMissing, 0, wildfireAt, false, terminal);
        if (time + MachinistRules.EffectMargin < horizon)
        {
            if (first.Action != 0) Apply(first.Action, first.Tool, first.RecoveryWildfire);
            else if (off) s.Weaves = s.WeaveLimit; // 等待候选放弃本G剩余插槽，必须推进。
        }
        var steps = 0;
        for (; time < horizon - .001f && steps < 10000; steps++)
        {
            if (s.GcdLeft <= .001f)
            {
                if (s.Lock > .001f) { Advance(s.Lock); continue; }
                if (time + MachinistRules.EffectMargin >= horizon) { Advance(horizon - time); break; }
                Apply(NextGcd(s, policy, reserved).Action);
                continue;
            }
            if (s.Lock <= .001f && MachinistRules.Slots(s) > 0 && s.GcdLeft > MachinistWildfire.SubmitTail(s))
            {
                var choice = NextOff(s, policy);
                if (choice.Action != 0)
                {
                    Advance(Math.Max(0, choice.Delay - (s.GcdTotal - s.GcdLeft)));
                    if (time + MachinistRules.EffectMargin < horizon) { Apply(choice.Action, choice.Tool, choice.RecoveryWildfire); continue; }
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
        var resourceCost = batteryMissing * MachinistQueen.FullPotency / 50
            + HeatCost(initial, heatMissing);
        return new(s, damage, heatLost, batteryLost, heatMissing, batteryMissing, endMissing, wildfireAt,
            time >= horizon - .001f && steps < 10000, terminal)
        { DrillLost = drillLost, ToolDelay = toolDelay, Tools = tools, HeatCycles = heatCycles,
            HyperchargeAt = hyperchargeAt, ResourceCost = resourceCost };
    }

    private static float HeatCost(MachinistState initial, int missing)
    {
        if (missing <= 0) return 0;
        var s = initial with { Reassemble = 0, ComboLeft = 30 };
        var aoe = s.HeatAction == MCHSkill.自动弩;
        var filler = aoe ? MachinistDamage.Action(s, MCHSkill.霰弹枪)
            : (MachinistDamage.Action(s, MCHSkill.热分裂弹1)
                + MachinistDamage.Action(s with { Combo = MCHSkill.热分裂弹1 }, MCHSkill.热独头弹2)
                + MachinistDamage.Action(s with { Combo = MCHSkill.热独头弹2 }, MCHSkill.热狙击弹3)) / 3;
        var refunds = aoe ? 0 : 2.5f * (MachinistDamage.Action(s, MCHSkill.双将) + MachinistDamage.Action(s, MCHSkill.将死));
        // 每50热按五发过热+返还能技，减去7.5秒普通填充的净威力估值。
        // 50热门槛失守至少计一组，其余缺口按比例；代价有限，允许为更高窗口收益让步。
        var cycle = Math.Max(0, 5 * MachinistDamage.Action(s, s.HeatAction) + refunds - 7.5f / Math.Max(1, s.Gcd) * filler);
        var lostCycles = initial.GoalHeat / 50 - Math.Max(0, initial.GoalHeat - missing) / 50;
        return cycle * Math.Max(missing / 50f, lostCycles);
    }
}
