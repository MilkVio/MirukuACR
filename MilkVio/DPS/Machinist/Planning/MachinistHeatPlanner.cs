using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Planning;

// 热量与电量时序共用此规则。只推演值快照，不保存跨目标/死亡/转场的爆发承诺。
internal static class MachinistHeatPlanner
{
    internal static bool PrepareBridge(MachinistState s)
    {
        if (s.Level != 100 || s.WindowActive || s.FastBurst || s.AoeQt && s.Targets >= 3
            || !s.WildfireQt || !s.HyperchargeQt || s.Reassemble > 0 || s.Heated
            || s.WildfireCd <= 0 || s.WildfireCd > 4 * s.Gcd || s.Heat < 40 || s.Heat >= 95) return false;
        var free = s.FreeHypercharge > s.WildfireCd || s.BarrelQt && s.BarrelCd <= s.WildfireCd;
        if (!free) return false;
        // 只挪动不会满充能的现有钻头，在团辅前续连击/补热，留作两次超荷间的普通G。
        // 第一轮热冲击及桥接之前不得溢出钻头；两轮结束后仍须能续上新刷新的连击。
        var bridgeAt = s.WildfireCd + MachinistRules.HeatCycle + s.Gcd;
        var comboAt = bridgeAt + MachinistRules.HeatCycle + s.Gcd;
        return (2 - s.Drill) * s.DrillRecast > bridgeAt + MachinistRules.EffectMargin
            && comboAt + MachinistRules.EffectMargin < 30;
    }

    // 只验算当前候选及其恢复；窗口的资源交付/快速野火仍由各自规划决定。
    internal static bool ComboFirst(MachinistState initial, uint first, out float resume)
    {
        var s = initial;
        MachinistModel.Apply(ref s, first);
        var wait = Math.Max(s.Lock, s.HyperchargeCd);
        var ready = s;
        MachinistModel.Advance(ref ready, wait);
        // 与当前团辅的热量许可一致：野火未开时由野火路线决定首轮超荷，不能在此抢排。
        if (!initial.WindowActive && !initial.FastBurst && initial.Level == 100
            && (!initial.WildfireQt || initial.WildfireCd > 100)
            && MachinistRules.BurstLeft(initial) > 0 && ready.CanHypercharge
            && ready.GcdLeft > MachinistWildfire.SubmitTail(ready) && MachinistRules.Slots(ready) > 0
            && Fits(ready with { Combo = 0, ComboLeft = 0 }))
        {
            var fits = Fits(ready, out resume);
            resume += wait;
            return !fits;
        }
        // 没有可接的第二轮过热时，只看工具/预备之后实际何时恢复连击。
        return !Recover(s, initial.ComboNext, out resume);
    }

    internal static bool Fits(MachinistState initial) => Fits(initial, out _);

    private static bool Fits(MachinistState initial, out float resume)
    {
        resume = initial.GcdLeft + MachinistRules.HeatCycle;
        if (!MachinistRules.HeatFits(initial)
            && (initial.Level != 100 || MachinistRules.BurstLeft(initial) <= 0 || !initial.CanHypercharge)) return false;
        var duration = initial.GcdLeft + MachinistRules.HeatCycle;
        // 基础路线不靠延后锚/锯或溢出钻头换第二次超荷。
        if (initial.AnchorCd < duration - .1f || initial.SawCd < duration - .1f
            || (2 - initial.Drill) * initial.DrillRecast < duration - .1f) return false;
        var s = initial;
        MachinistModel.Apply(ref s, MCHSkill.超荷);
        return Recover(s, initial.ComboNext, out resume);
    }

    private static bool Recover(MachinistState initial, uint combo, out float resume)
    {
        var s = initial;
        var left = MachinistRules.BurstLeft(initial);
        var elapsed = 0f;
        resume = 0;
        var continued = combo == 0;
        for (var i = 0; i < 12; i++)
        {
            var dt = s.GcdLeft;
            if (!continued) resume = elapsed + dt;
            if (!continued && (s.ComboNext != combo || s.ComboLeft <= dt + MachinistRules.EffectMargin)
                || s.Excavator > 0 && s.Excavator <= dt + MachinistRules.EffectMargin
                || s.FullMetalPending && s.FullMetal <= dt + MachinistRules.EffectMargin) return false;
            MachinistModel.Advance(ref s, dt); elapsed += dt;
            var action = MachinistRules.NormalGcd(s, planCombo: false).Action;
            // 有高威力预备时，不能把它从当前增益里挤出去来换热冲击覆盖。
            if ((action == MCHSkill.全金属爆发 && initial.FullMetalPending
                || action == MCHSkill.掘地飞轮 && initial.Excavator > 0)
                && left > 0 && elapsed + MachinistRules.EffectMargin >= left) return false;
            if (action == combo) continued = true;
            MachinistModel.Apply(ref s, action);
            if (!s.Heated && s.Excavator <= 0 && !s.FullMetalPending && continued) return true;
        }
        return false;
    }

    internal static bool Reserve(MachinistState s, out string reason)
    {
        reason = "";
        var at = MachinistRules.BurstAt(s);
        if (s.Level != 100 || s.WindowActive || s.FastBurst || s.IsDump || !s.HyperchargeQt
            || s.AoeQt && s.Targets >= 3 || s.FreeHypercharge > 0
            || !float.IsFinite(at) || at <= 0 || MachinistRules.BurstLeft(s) > 0) return false;
        // 自身120的目标是两轮可完整执行、至少部分落入窗口的过热。多推演一轮尾部，
        // 避免只看进入时的50热或窗口内发数，漏掉续连击/工具期限及第二轮能否打完。
        // 机器人只分配同一路线产生的电量和合法插槽，不递归调用热量许可。
        var horizon = at + 20 + MachinistRules.HeatCycle + s.Gcd;
        var kept = MachinistProjection.Forecast(s, horizon, holdQueen: true, burstHeat: true, holdHeat: true);
        var spent = MachinistProjection.Forecast(s, horizon, MCHSkill.超荷, holdQueen: true, burstHeat: true, holdHeat: true);
        var comparison = $"自身120距{at:F1}s；支出/保留：过热{spent.BurstCycles}/{kept.BurstCycles}轮，"
            + $"烈焰弹{spent.BurstShots}/{kept.BurstShots}发，机器人{spent.QueenBattery}/{kept.QueenBattery}电，"
            + $"溢热{spent.HeatOverflow}/{kept.HeatOverflow}，溢电{spent.QueenLost}/{kept.QueenLost}";
        // 预测末端不是斩杀点；不能用截止时已打完的轮数差认定整场少一次过热。
        // 热量溢出/免费预备期限由实际路线检查；已知阶段期限交给输出窗口规划。
        string? release = !kept.Complete || !spent.Complete ? "预测未完成，按实况重算"
            : spent.BurstCycles >= 2 ? "支出后仍能双过热"
            : kept.BurstCycles < 2 ? "保留也无法安排第二轮，保护野火后正常支出"
            : kept.HeatOverflow > spent.HeatOverflow ? "保留会额外溢热"
            : kept.WildfireAt > spent.WildfireAt + .05f ? "保留路线会推迟野火"
            : kept.QueenLost > spent.QueenLost ? "保留会额外溢电"
            : s.QueenQt && kept.QueenBattery < Math.Min(80, spent.QueenBattery) ? "保留会损失本轮机器人或超过允许的电量取舍"
            : null;
        reason = release != null ? $"{release}；{comparison}" : $"为自身120双过热留热；{comparison}";
        return release == null;
    }
}
