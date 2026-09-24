using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Planning;

// 只比较眼前一轮野火的兑现，期限内继续输出，不以六击或下一轮必定结算作为开火条件。
// 不调用资源预算/窗口求解，避免窗口候选与短路线相互递归；期末资源仍由窗口规划联合校验。
internal static class MachinistFastBurst
{
    internal readonly record struct Result(int Hits, float Damage);

    internal static bool CanHypercharge(MachinistState s)
    {
        if (!s.FastWildfireActive || !s.CanHypercharge || s.WildfireHits >= 6
            || s.WildfireLeft <= s.GcdLeft + MachinistRules.EffectMargin) return false;
        return Better(Project(s, MCHSkill.超荷, true), Project(s, 0, false));
    }

    internal static MachinistChoice Gcd(MachinistState initial, MachinistChoice normal, uint held)
    {
        if (!initial.FastBurst && !initial.FastWildfireActive) return normal;
        var s = initial;
        MachinistModel.Advance(ref s, s.GcdLeft);
        if (s.Heated || s.Reassemble > 0 || held != 0 || !s.HyperchargeQt || s.Heat >= 50
            || s.FreeHypercharge > 0 || s.SawFirst && MachinistRules.ToolReady(s, MCHSkill.回转飞锯)) return normal;
        var start = Math.Max(Math.Max(.3f, s.WeaveLock), s.WildfireCd);
        if (!s.FastWildfireActive && (!s.FastBurst || !s.WildfireQt || s.WildfireLeft > 0
            || start >= s.Gcd - MachinistWildfire.SubmitTail(s)
            || s.WindowActive && s.WindowLeft < start + 10 + MachinistRules.EffectMargin)) return normal;
        var combo = MachinistRules.Combo(s);
        if (combo == normal.Action) return normal;
        var a = Project(s, combo, true);
        var b = Project(s, normal.Action, true);
        return Better(a, b) ? new(combo, $"快速野火：补热衔接，预计{a.Hits}/6击（工具路线{b.Hits}/6）") : normal;
    }

    internal static Result Preview(MachinistState s) => Project(s, MCHSkill.野火, true);

    internal static Result AfterGcd(MachinistState s, uint action)
    {
        MachinistModel.Advance(ref s, s.GcdLeft);
        return Project(s, action, true);
    }

    private static bool Better(Result a, Result b) => a.Hits > b.Hits
        || a.Hits == b.Hits && a.Damage > b.Damage + .5f;

    private static Result Project(MachinistState initial, uint first, bool heat)
    {
        var s = initial;
        var time = 0f;
        var damage = 0f;
        var fired = s.WildfireLeft > 0;
        var hits = fired ? s.WildfireHits : 0;
        var horizon = fired ? s.WildfireLeft : Math.Max(s.WildfireCd, Math.Max(.3f, s.WeaveLock)) + 10;
        if (s.WindowActive) horizon = Math.Min(horizon, s.WindowLeft);
        void Apply(uint action)
        {
            if (time + MachinistRules.EffectMargin < horizon)
                damage += MachinistDamage.Action(s, action);
            MachinistModel.Apply(ref s, action);
            if (action == MCHSkill.野火) { fired = true; s.WildfireLead = false; hits = 0; }
            if (fired) hits = Math.Max(hits, s.WildfireHits);
        }
        if (first != 0) Apply(first);
        for (var steps = 0; steps < 160 && time < horizon - .001f; steps++)
        {
            if (s.Lock <= .001f && s.GcdLeft <= .001f)
            {
                if (time + MachinistRules.EffectMargin >= horizon) break;
                var next = MachinistRules.NormalGcd(s).Action;
                var combo = MachinistRules.Combo(s);
                if (heat && !s.Heated && s.Reassemble <= 0 && s.FreeHypercharge <= 0 && s.Heat < 50
                    && s.Heat + MachinistModel.HeatGain(combo) >= 50 && s.HyperchargeQt)
                    next = combo;
                Apply(next);
                continue;
            }
            if (s.Lock <= .001f && MachinistRules.Slots(s) > 0 && s.GcdLeft > MachinistWildfire.SubmitTail(s))
            {
                uint action = 0;
                // 本G即将转好的野火也要保留插槽，与实际逐帧提交规则一致。
                if (!fired && MachinistWildfire.TryPlan(s, out var fire, out _))
                {
                    if (fire.StartIn <= .001f) action = MCHSkill.野火;
                }
                else if (fired && heat && s.CanHypercharge && s.WildfireLeft > s.GcdLeft + MachinistRules.EffectMargin)
                    action = MCHSkill.超荷;
                else if (fired && heat && s.HyperchargeQt && s.BarrelQt && s.BarrelCd <= 0
                    && !s.Heated && s.Heat < 50 && s.FreeHypercharge <= 0 && s.FullMetal <= 0)
                    action = MCHSkill.枪管加热;
                else if (s.Gauss >= 1) action = MCHSkill.双将;
                else if (s.Ricochet >= 1) action = MCHSkill.将死;
                if (action != 0) { Apply(action); continue; }
            }
            var dt = s.Lock > .001f ? s.GcdLeft > .001f ? Math.Min(s.Lock, s.GcdLeft) : s.Lock : s.GcdLeft;
            if (!fired && s.WildfireCd > .001f) dt = Math.Min(dt, s.WildfireCd);
            if (heat && s.HyperchargeCd > .001f) dt = Math.Min(dt, s.HyperchargeCd);
            if (heat && s.BarrelQt && s.BarrelCd > .001f) dt = Math.Min(dt, s.BarrelCd);
            dt = Math.Min(Math.Max(.001f, dt), horizon - time);
            MachinistModel.Advance(ref s, dt); time += dt;
        }
        return new(hits, damage);
    }
}
