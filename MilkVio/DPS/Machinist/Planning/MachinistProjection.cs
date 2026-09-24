using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Planning;

internal readonly record struct MachinistForecast(MachinistState End, int HeatOverflow, int BatteryOverflow,
    float Reached100, float BurstBattery, float WildfireAt)
{
    internal float BurstBonus { get; init; }
    internal int BurstShots { get; init; }
}
internal readonly record struct MachinistBudget(bool Queen, bool Heat, bool WildfireHeat, string Reason)
{
    internal float QueenDelay { get; init; }
    internal long GcdEnd { get; init; }
    internal int Battery { get; init; }
    internal MachinistChoice? WindowChoice { get; init; }
    internal int WindowVersion { get; init; }
    internal int QtKey { get; init; }
    internal ulong Target { get; init; }
}

internal static class MachinistProjection
{
    public static MachinistBudget Evaluate(MachinistState s)
    {
        if (!s.Alive || !s.HasTarget) return new(false, false, false, "自身/目标不可用，等待实况恢复");
        if (s.WindowActive)
        {
            var plan = MachinistWindowPlanner.Choose(s, true);
            return new(plan.Choice.Action == MCHSkill.后式自走人偶, plan.Choice.Action == MCHSkill.超荷,
                plan.Choice.Action is MCHSkill.超荷 or MCHSkill.野火, plan.Reason)
            { WindowChoice = plan.Choice, WindowVersion = s.WindowVersion, QtKey = s.QtKey, Target = s.TargetId };
        }
        var heat = CanSpendHeat(s, out var heatReason);
        var queen = QueenPlan(s);
        var wildfireReason = "";
        if (MachinistWildfire.Preparing(s))
        {
            MachinistWildfire.TryPlan(s, out _, out var routeReason);
            wildfireReason = $"；野火：{routeReason}";
        }
        return new(queen.Summon, heat, true, $"热量：{heatReason}；电量：{queen.Reason}{wildfireReason}")
        { QueenDelay = queen.Summon ? s.GcdTotal - s.GcdLeft + queen.At : 0,
            GcdEnd = s.Now + (long)(s.GcdLeft * 1000), Battery = s.Battery,
            QtKey = s.QtKey, Target = s.TargetId, WindowVersion = s.WindowVersion };
    }

    public static bool CanSpendHeat(MachinistState s, out string reason) => CanSpendHeat(s, out reason, out _);

    internal static bool CanSpendHeat(MachinistState s, out string reason, out bool revisitTiming)
    {
        revisitTiming = false;
        if (!s.Alive || !s.HasTarget) { reason = "自身/目标不可用"; return false; }
        if (s.WindowActive) { var budget = Evaluate(s); reason = budget.Reason; return budget.Heat; }
        if (s.FastWildfireActive)
        {
            var use = MachinistFastBurst.CanHypercharge(s);
            reason = use ? "快速野火：超荷可增加本轮兑现" : "快速野火：资源不足或剩余时间不宜超荷，继续GCD";
            return use;
        }
        reason = "不可超荷/工具尚未安排";
        if (!s.CanHypercharge || !MachinistHeatPlanner.Fits(s)) return false;
        if (s.WildfireLeft > 0 || s.IsDump
            || (!s.WildfireQt || s.WildfireCd > 100) && MachinistRules.BurstLeft(s) > 0)
        { reason = "当前兑现窗口，过热后预备与连击可及时恢复"; return true; }
        if (!s.WildfireQt)
        {
            if (MachinistHeatPlanner.Reserve(s, out reason)) return false;
            reason = "野火QT关闭，无野火热量硬性预留"; return true;
        }
        if (s.WildfireCd <= 0) { reason = "由野火路线安排超荷"; return false; }
        var after = HeatForecast(s, s.WildfireCd, MCHSkill.超荷).End;
        var free = after.FreeHypercharge > 0 || after.BarrelQt && after.BarrelCd <= .01f;
        // 预测可能已在转好前交出本轮超荷；这份资源已经进入野火安排，不能再要求一份新的50热。
        var prepared = after.WildfireCd > 100 || after.Heated && after.OverheatStacks == 5
            && MachinistWildfire.TryPlan(after, out _, out _);
        if (!prepared && (after.HyperchargeCd > .01f || after.Heated || !free && after.Heat < 50))
        {
            reason = $"会推迟野火：预计热{after.Heat} 免费={free} 超荷CD={after.HyperchargeCd:F1}";
            return false;
        }
        if (s.WildfireCd <= 30)
        {
            // 枪管即使在野火前转好，也可能占用同G唯一剩下的插槽。
            // 比较真正开火时间，不能只在冷却零点看到免费Buff就认为没有推迟。
            var horizon = s.WildfireCd + 2 * s.Gcd;
            var kept = HeatForecast(s, horizon).WildfireAt;
            var spent = HeatForecast(s, horizon, MCHSkill.超荷).WildfireAt;
            if (float.IsFinite(kept) && spent > kept + .05f)
            { revisitTiming = true; reason = "保留热量可更早开野火，枪管/插槽不能及时补位"; return false; }
        }
        if (MachinistHeatPlanner.Reserve(s, out reason)) return false;
        reason = $"可支出；下次野火预计热{after.Heat} 免费={free}";
        return true;
    }

    // 热量可达性不替电量规划擅自召唤机器人，否则满电快照会凭空挤掉未来野火插槽。
    private static MachinistForecast HeatForecast(MachinistState s, float horizon, uint first = 0) =>
        Forecast(s, horizon, first, holdQueen: true);

    public static bool CanSummon(MachinistState s, out string reason)
    {
        if (s.WindowActive) { var budget = Evaluate(s); reason = budget.Reason; return budget.Queen; }
        var plan = QueenPlan(s);
        reason = plan.Reason;
        return plan.Summon;
    }

    private static MachinistBatteryPlanner.Advice QueenPlan(MachinistState s)
    {
        if (!s.Alive || !s.HasTarget) return new(false, 0, 0, 0, "自身/目标不可用");
        if (!s.QueenQt || s.Battery < 50 || s.QueenLeft > 0 || s.QueenCd > 0)
            return new(false, 0, 0, 0, "机器人QT关闭/电量不足/机器人仍在场");
        if (s.DumpQt) return new(true, 0, s.Battery, 0, "倾泻QT：当前可召唤");
        return MachinistBatteryPlanner.Choose(s);
    }

    internal static float QueenCoverage(float summonAt, float windowLeft, float start = 0) =>
        MachinistQueen.Coverage(summonAt, windowLeft, start);

    internal static MachinistChoice SelectOff(MachinistState s, MachinistBudget budget)
    {
        if (!s.Alive || !s.HasTarget) return new(0, "自身/目标不可用");
        if (!s.WindowActive)
        {
            if (budget.WindowChoice != null || budget.Target != 0 && (budget.Target != s.TargetId
                || budget.QtKey != s.QtKey || budget.WindowVersion != s.WindowVersion || budget.Battery != s.Battery
                || budget.QueenDelay > 0 && Math.Abs(budget.GcdEnd - s.Now - (long)(s.GcdLeft * 1000)) > 100))
                budget = Evaluate(s);
            var normal = MachinistRules.Off(s, budget.Queen, budget.Heat, budget.WildfireHeat);
            return normal.Action == MCHSkill.后式自走人偶 ? normal with { Delay = budget.QueenDelay } : normal;
        }
        if (budget.WindowChoice is not { } choice) return MachinistWindowPlanner.Choose(s, true).Choice;
        // 缓存只保存候选，不授予跨QT/目标/窗口的执行许可。宿主无法撤回已提交动作，提交前再核对。
        if (budget.WindowVersion != s.WindowVersion || budget.QtKey != s.QtKey || budget.Target != s.TargetId
            || !WindowChoiceUsable(s, choice))
            return MachinistWindowPlanner.Choose(s, true).Choice;
        return choice;
    }

    private static bool WindowChoiceUsable(MachinistState s, MachinistChoice choice) => choice.Action switch
    {
        MCHSkill.超荷 => s.CanHypercharge,
        MCHSkill.后式自走人偶 => s.QueenQt && s.Battery >= 50 && s.QueenLeft <= 0 && s.QueenCd <= 0
            && s.WindowLeft > MachinistQueen.FirstHit + MachinistRules.EffectMargin,
        MCHSkill.野火 => s.WildfireQt && s.WildfireLeft <= 0 && (!s.HyperchargeQt
            ? s.WindowLeft >= 10 + MachinistRules.EffectMargin
            : MachinistWildfire.TryPlan(s, out var plan, out _) && plan.Choice(s).Action == MCHSkill.野火),
        MCHSkill.枪管加热 => s.BarrelQt && s.InCombat && s.FreeHypercharge <= 0 && s.FullMetal <= 0,
        MCHSkill.整备 => s.ReassembleQt && s.ReassembleCharges >= 1 && s.Reassemble <= 0 && !s.Heated,
        MCHSkill.双将 => s.Gauss >= 1,
        MCHSkill.将死 => s.Ricochet >= 1,
        _ => choice.Action == 0
    };

    internal static MachinistChoice SelectGcd(MachinistState s, uint reserved = 0, uint held = 0) =>
        s.WindowActive && s.Level == 100 ? MachinistWindowPlanner.Choose(s, false, reserved, held).Choice
            : MachinistRules.Gcd(s, reserved, held);

    // 事件步进：公共技能、能力技、冷却与Buff使用同一规则。资源保留预测不递归调用自己。
    public static MachinistForecast Forecast(MachinistState initial, float horizon, uint first = 0, bool holdQueen = false,
        bool planHeat = false, float? burstAt = null, bool burstHeat = false, bool holdHeat = false)
    {
        var s = initial;
        horizon = Math.Clamp(float.IsFinite(horizon) ? horizon : 120, 0, 86400);
        var elapsed = 0f;
        var heatOverflow = 0;
        var batteryOverflow = 0;
        var burstBonus = 0f;
        var burstShots = 0;
        var batteryEvents = burstHeat && initial.QueenQt ? new List<MachinistBatteryPlanner.Event>() : null;
        var reached100 = s.Battery == 100 ? 0 : float.PositiveInfinity;
        var wildfireAt = first == MCHSkill.野火 ? 0 : float.PositiveInfinity;
        var (burstStart, burstEnd) = MachinistRules.QueenWindow(initial);
        if (burstAt.HasValue) { burstStart = burstAt.Value; burstEnd = burstStart + 20; }
        var burstBattery = first == MCHSkill.后式自走人偶 ? s.Battery * QueenCoverage(0, burstEnd, burstStart) : 0;
        void Apply(uint action)
        {
            if (burstHeat)
                burstBonus += MachinistDamage.Action(s, action)
                    * (MachinistDamage.Multiplier(initial, elapsed + MachinistRules.EffectMargin) - 1);
            if (MachinistModel.BatteryGain(action) is var gain && gain > 0)
                batteryEvents?.Add(new(elapsed, gain, false));
            if (MachinistRules.IsHeatShot(action) && elapsed + MachinistRules.EffectMargin >= burstStart
                && elapsed + MachinistRules.EffectMargin < burstEnd) burstShots++;
            MachinistModel.Apply(ref s, action);
        }
        if (first != 0) Apply(first);
        // 一次预测只保留当前窗口约束；预测结束不读取/修改真实窗口。
        for (var steps = 0; steps < Math.Max(1600, (int)(horizon * 20)) && elapsed < horizon - .001f; steps++)
        {
            if (s.GcdLeft <= .001f)
            {
                var action = MachinistRules.Gcd(s, planBattery: false).Action;
                heatOverflow += Math.Max(0, s.Heat + MachinistModel.HeatGain(action) - 100);
                batteryOverflow += Math.Max(0, s.Battery + MachinistModel.BatteryGain(action) - 100);
                Apply(action);
                if (s.Battery == 100) reached100 = Math.Min(reached100, elapsed);
                continue;
            }
            if (s.Lock <= .001f && MachinistRules.Slots(s) > 0)
            {
                var next = MachinistRules.Gcd(s, planBattery: false).Action;
                var queen = !holdQueen && s.QueenQt && s.Battery >= 50 && s.GoalBattery == 0
                    && (s.Battery == 100 || s.Battery + MachinistModel.BatteryGain(next) > 100);
                // 保存一份野火资源，其余接近溢出的热量可支出，免费Buff也不能预测成永久存在。
                var heat = planHeat && !s.WindowActive ? CanSpendHeat(s, out _)
                    : s.GoalHeat == 0 && (!holdHeat && (s.Heat >= 95 || s.FreeHypercharge > 0 && s.FreeHypercharge < 10)
                        || burstHeat && (!s.WildfireQt || s.WildfireCd > 100) && MachinistRules.BurstLeft(s) > 0)
                        && (!s.WildfireQt || s.WildfireCd > 12);
                if (batteryEvents != null && s.QueenQt
                    && MachinistRules.Off(s with { Battery = 100, QueenLeft = 0, QueenCd = 0 }, true, heat, planBattery: false).Action == MCHSkill.后式自走人偶)
                {
                    batteryEvents.Add(new(elapsed, 0, true));
                    var alignment = burstStart - MachinistQueen.FirstHit + .03f;
                    if (alignment > elapsed + .001f && alignment < elapsed + s.GcdLeft - MachinistWildfire.SubmitTail(s) - .03f)
                        batteryEvents.Add(new(alignment, 0, true));
                }
                var off = MachinistRules.Off(s, queen, heat, planBattery: false);
                if (off.Action != 0)
                {
                    var wait = Math.Max(0, off.Delay - (s.GcdTotal - s.GcdLeft));
                    if (wait > 0)
                    {
                        var dt = Math.Min(wait, horizon - elapsed);
                        MachinistModel.Advance(ref s, dt); elapsed += dt;
                    }
                    if (elapsed < horizon - .001f)
                    {
                        if (off.Action == MCHSkill.后式自走人偶) burstBattery += s.Battery * QueenCoverage(elapsed, burstEnd, burstStart);
                        if (off.Action == MCHSkill.野火) wildfireAt = Math.Min(wildfireAt, elapsed);
                        Apply(off.Action);
                    }
                    continue;
                }
            }
            var step = s.Lock > .001f ? Math.Min(s.Lock, s.GcdLeft) : s.GcdLeft;
            // 无动作时也必须在即将转好的野火处醒来，不能一口气跳到下一G。
            if (s.WildfireQt && s.WildfireCd > .001f) step = Math.Min(step, s.WildfireCd);
            step = Math.Min(Math.Max(.001f, step), horizon - elapsed);
            MachinistModel.Advance(ref s, step); elapsed += step;
        }
        // 预测末量谱与已召出的机器人分开记账，否则提前召出100电会被误判为“期末只有10电”。
        if (s.Battery >= 50) burstBattery += s.Battery * QueenCoverage(horizon + s.QueenLeft, burstEnd, burstStart);
        // 由可用插槽独立分配电量，不把“预测暂不召唤”造成的满电算成实战损失。
        if (batteryEvents != null)
        {
            var queenBonus = MachinistBatteryPlanner.Allocate(initial, batteryEvents).Value * MachinistQueen.FullPotency / 50;
            burstBonus += queenBonus;
        }
        return new(s, heatOverflow, batteryOverflow, reached100, burstBattery, wildfireAt)
        { BurstBonus = burstBonus, BurstShots = burstShots };
    }
}
