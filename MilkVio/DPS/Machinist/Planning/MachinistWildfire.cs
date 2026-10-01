using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Planning;

internal enum MachinistWildfireRoute { None, Pair, Lead, Follow, Fast, Recover }

internal readonly record struct MachinistWildfirePlan(MachinistWildfireRoute Route, float StartIn,
    float FireIn, float SixthIn, float ToolDelay, uint Recovery, float RecoveryIn)
{
    public int Hits { get; init; }
    public string Name => Route switch
    {
        MachinistWildfireRoute.Pair => "超荷后野火，五热冲击加一普通GCD",
        MachinistWildfireRoute.Lead => "后段野火，下一普通GCD后超荷",
        MachinistWildfireRoute.Fast => "快速野火：首个合法插槽开火，再按资源兑现",
        MachinistWildfireRoute.Recover => "野火恢复：按现有过热补挂，不等待新的超荷资源",
        _ => "超荷已就绪，接野火"
    };

    public MachinistChoice Choice(MachinistState s) => new(
        Route == MachinistWildfireRoute.Pair ? MCHSkill.超荷 : MCHSkill.野火,
        Name, s.GcdTotal - s.GcdLeft + StartIn) { RecoveryWildfire = Route == MachinistWildfireRoute.Recover };
}

// 只预测本G能启动的野火及其后续兑现，不递归调用资源预算或oGCD求解。
// 普通过热仍使用HeatFits；野火以六击、预备期限和插槽为硬约束，再比较最早开火时间。
internal static class MachinistWildfire
{
    private const float HitMargin = MachinistRules.EffectMargin + .15f;
    // 与Slots的0.15秒尾部余量一致，不能规划一个等到最后却已没有插槽的野火。
    internal static float SubmitTail(MachinistState s) => Math.Max(.5f, s.WeaveLock) + .15f;

    public static bool Preparing(MachinistState s) => s.WildfireQt && (s.HyperchargeQt || s.FastBurst)
        && s.WildfireLeft <= 0 && s.WildfireCd <= s.GcdLeft + s.Gcd
        && (!s.WindowActive || s.WindowLeft >= s.WildfireCd + 10 + MachinistRules.EffectMargin);

    public static bool TryPlan(MachinistState s, out MachinistWildfirePlan plan, out string reason)
    {
        plan = default;
        reason = "尚未进入本G野火准备时间";
        if (s.FastBurst) return TryFast(s, out plan, out reason);
        if (!s.WildfireQt || s.WildfireLeft > 0 || s.WildfireCd >= s.GcdLeft) return false;
        if (s.Heated)
        {
            if (s.GcdTotal >= 1.8f && TryRoute(s, MachinistWildfireRoute.Follow, out plan, out reason)) return true;
            return TryRecover(s, out plan, out reason);
        }
        if (s.GcdTotal < 1.8f) return TryRecover(s, out plan, out reason);
        if (!s.HyperchargeQt) { reason = "超荷QT关闭"; return false; }
        // 已生效整备可能绑定另一个工具，不能用普通GCD优先级猜它将消耗哪一个预备。
        if (s.Reassemble > 0) { reason = "先兑现已生效整备"; return false; }
        var pair = TryRoute(s, MachinistWildfireRoute.Pair, out var paired, out var pairReason);
        var lead = TryRoute(s, MachinistWildfireRoute.Lead, out var leading, out var leadReason);
        if (!pair && !lead)
        {
            if (RecoveryNeeded(s)) return TryRecover(s, out plan, out reason);
            reason = $"双插：{pairReason}；前置：{leadReason}"; return false;
        }
        // 工具冷却推迟只是同期开火时的次级代价，不能让工具的普通排序反过来推迟野火。
        plan = pair && (!lead || paired.FireIn < leading.FireIn - .001f
            || paired.FireIn <= leading.FireIn + .001f && paired.ToolDelay <= leading.ToolDelay) ? paired : leading;
        reason = plan.Name;
        return true;
    }

    // 只识别已经实际付出的超荷，不把普通缺热或单纯野火就绪变成快速爆发。
    internal static bool RecoveryNeeded(MachinistState s) => s.Level == 100 && !s.FastBurst
        && (s.Heated || s.HyperchargeCd > .001f || s.GcdTotal < 1.8f || s.HyperchargeRecoveryLeft > 0);

    // 冷却归零后再覆盖两个普通G的插槽交接；不能在等待合法插槽时遗失补挂资格。
    internal static float RecoveryWindow(float gcd) => 10 + 2 * Math.Max(1, gcd);

    private static bool TryRecover(MachinistState initial, out MachinistWildfirePlan plan, out string reason)
    {
        plan = default;
        reason = "野火恢复：本G没有合法插槽";
        if (!RecoveryNeeded(initial) || initial.Weaves >= initial.WeaveLimit) return false;
        var fire = Math.Max(initial.Lock, initial.WildfireCd);
        var s = initial;
        MachinistModel.Advance(ref s, fire);
        if (s.GcdLeft <= SubmitTail(s) || MachinistRules.Slots(s) == 0) return false;
        if (initial.WindowActive && initial.WindowLeft < fire + 10 + MachinistRules.EffectMargin)
        { reason = "野火恢复：窗口不足本次结算"; return false; }
        MachinistModel.Apply(ref s, MCHSkill.野火, recoveryWildfire: true);
        var elapsed = fire;
        var hits = 0;
        var sixth = float.PositiveInfinity;
        uint recovery = 0;
        var recoveryAt = 0f;
        // 剩几层就演算几层，之后直接推进真实普通G；不为凑六击虚构新的超荷。
        for (var i = 0; i < 8; i++)
        {
            var dt = Math.Max(s.GcdLeft, s.Lock);
            if (elapsed + dt + HitMargin >= fire + 10) break;
            MachinistModel.Advance(ref s, dt); elapsed += dt;
            var action = MachinistRules.NormalGcd(s).Action;
            if (!MachinistRules.IsHeatShot(action) && recovery == 0) { recovery = action; recoveryAt = elapsed; }
            MachinistModel.Apply(ref s, action);
            if (++hits == 6) { sixth = elapsed; break; }
        }
        plan = new(MachinistWildfireRoute.Recover, fire, fire, sixth, 0, recovery, recoveryAt) { Hits = hits };
        reason = $"{plan.Name}，预计{hits}/6击";
        return true;
    }

    private static bool TryFast(MachinistState s, out MachinistWildfirePlan plan, out string reason)
    {
        plan = default;
        reason = "快速野火：QT关闭/已有野火/本G没有合法时点";
        if (!s.WildfireQt || s.WildfireLeft > 0 || s.Weaves >= s.WeaveLimit) return false;
        var fire = Math.Max(s.Lock, s.WildfireCd);
        var ready = s;
        MachinistModel.Advance(ref ready, fire);
        if (ready.GcdLeft <= SubmitTail(ready) || MachinistRules.Slots(ready) == 0) return false;
        if (s.WindowActive && s.WindowLeft < fire + 10 + MachinistRules.EffectMargin)
        { reason = "快速野火：输出窗口不足本次结算"; return false; }
        plan = new(MachinistWildfireRoute.Fast, fire, fire, float.PositiveInfinity, 0, 0, 0);
        reason = plan.Name;
        return true;
    }

    private static bool TryRoute(MachinistState initial, MachinistWildfireRoute route,
        out MachinistWildfirePlan plan, out string reason)
    {
        plan = default;
        reason = "没有本G安全插槽";
        var s = initial;
        var weave = Math.Max(.3f, s.WeaveLock);
        var pair = route == MachinistWildfireRoute.Pair;
        var lead = route == MachinistWildfireRoute.Lead;
        if (s.WeaveLimit - s.Weaves < (pair ? 2 : 1)) return false;
        if (pair && !s.CanHypercharge) { reason = "超荷资源/冷却/整备不允许"; return false; }
        if (!pair && !lead && (!s.Heated || s.OverheatStacks != 5))
        { reason = "已消耗过热层数，不能保证五热加一普通GCD"; return false; }

        var sixth = s.GcdLeft + (lead ? s.Gcd + 6 : MachinistRules.HeatCycle);
        var fire = Math.Max(s.WildfireCd, Math.Max(s.Lock + (pair ? weave : 0), sixth + HitMargin - 10));
        var start = pair ? s.Lock : fire;
        if (s.GcdLeft - fire <= SubmitTail(s)) return false;
        if (s.WindowActive && s.WindowLeft < fire + 10 + MachinistRules.EffectMargin)
        { reason = "输出窗口不足完整野火"; return false; }

        var elapsed = 0f;
        var toolDelay = 0f;
        uint recovery = 0;
        var recoveryAt = 0f;
        var combo = initial.ComboNext;

        if (pair)
        {
            if (!Advance(ref s, start, ref elapsed, ref toolDelay, out reason)) return false;
            if (!s.CanHypercharge) { reason = "超荷预备在提交前到期"; return false; }
            MachinistModel.Apply(ref s, MCHSkill.超荷);
        }
        if (!Advance(ref s, fire - elapsed, ref elapsed, ref toolDelay, out reason)) return false;
        MachinistModel.Apply(ref s, MCHSkill.野火);
        if (lead)
        {
            if (!Advance(ref s, s.GcdLeft, ref elapsed, ref toolDelay, out reason)) return false;
            MachinistModel.Apply(ref s, MachinistRules.Gcd(s).Action);
            if (!Advance(ref s, Math.Max(s.Lock, s.HyperchargeCd), ref elapsed, ref toolDelay, out reason)) return false;
            if (!s.CanHypercharge || s.GcdLeft <= SubmitTail(s))
            { reason = "普通GCD后超荷资源/插槽不足"; return false; }
            MachinistModel.Apply(ref s, MCHSkill.超荷);
        }

        for (var i = 0; i < 5; i++)
        {
            if (!Advance(ref s, s.GcdLeft, ref elapsed, ref toolDelay, out reason)) return false;
            if (!s.Heated || s.Overheat <= MachinistRules.EffectMargin)
            { reason = "过热不足五次命中"; return false; }
            MachinistModel.Apply(ref s, s.HeatAction);
        }
        // 双插路线的第六击以及暂存工具必须实际排得下，不能只检查五发热冲击。
        for (var i = 0; i < 8; i++)
        {
            if ((lead || s.WildfireHits == 6) && s.Excavator <= 0 && !s.FullMetalPending
                && (combo == 0 || s.ComboNext != combo)) break;
            if (!Advance(ref s, s.GcdLeft, ref elapsed, ref toolDelay, out reason)) return false;
            var action = MachinistRules.Gcd(s).Action;
            if (recovery == 0) { recovery = action; recoveryAt = elapsed; }
            MachinistModel.Apply(ref s, action);
        }
        if (s.Excavator > 0 || s.FullMetalPending || combo != 0 && s.ComboNext == combo)
        { reason = "过热后工具/连击未能及时兑现"; return false; }
        if (s.WildfireHits != 6 || sixth - fire > 10 - HitMargin + .001f)
        { reason = "第六次命中没有足够余量"; return false; }
        plan = new(route, start, fire, sixth, toolDelay, recovery, recoveryAt);
        reason = plan.Name;
        return true;
    }

    private static bool Advance(ref MachinistState s, float seconds, ref float elapsed,
        ref float toolDelay, out string reason)
    {
        seconds = Math.Max(0, seconds);
        reason = "";
        if (s.Excavator > 0 && s.Excavator <= seconds + MachinistRules.EffectMargin)
            reason = "飞轮预备会到期";
        else if (s.FullMetalPending && s.FullMetal <= seconds + MachinistRules.EffectMargin)
            reason = "全金属预备会到期";
        else if (s.ComboNext != 0 && s.ComboLeft <= seconds + MachinistRules.EffectMargin)
            reason = "连击会中断";
        if (reason.Length != 0) return false;
        toolDelay += Math.Max(0, seconds - s.AnchorCd) + Math.Max(0, seconds - s.SawCd)
            + Math.Max(0, seconds - (2 - s.Drill) * s.DrillRecast);
        MachinistModel.Advance(ref s, seconds);
        elapsed += seconds;
        return true;
    }
}
