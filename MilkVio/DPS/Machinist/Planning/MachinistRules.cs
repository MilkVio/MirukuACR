using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Planning;

internal static class MachinistRules
{
    // 六次命中用生效余量验算，不把提交时间当作伤害时间。
    public const float EffectMargin = .35f;
    public const float HeatCycle = 7.5f;

    public static int Slots(MachinistState s) => Math.Max(0, Math.Min(s.WeaveLimit - s.Weaves,
        (int)((s.GcdLeft - s.Lock - .15f) / Math.Max(.3f, s.WeaveLock))));

    public static bool IsTool(uint id) => id is MCHSkill.钻头 or MCHSkill.空气锚 or MCHSkill.热弹
        or MCHSkill.回转飞锯 or MCHSkill.掘地飞轮;

    public static bool IsWeaponskill(uint id) => IsTool(id) || id is MCHSkill.全金属爆发
        or MCHSkill.热冲击 or MCHSkill.烈焰弹 or MCHSkill.自动弩 or MCHSkill.散射 or MCHSkill.霰弹枪
        or MCHSkill.热分裂弹1 or MCHSkill.热独头弹2 or MCHSkill.热狙击弹3
        or MCHSkill.分裂弹1 or MCHSkill.独头弹2 or MCHSkill.狙击弹3 or MCHSkill.毒菌冲击;

    public static bool IsHeatShot(uint id) => id is MCHSkill.热冲击 or MCHSkill.烈焰弹 or MCHSkill.自动弩;

    public static bool ToolReady(MachinistState s, uint id) => id switch
    {
        // 事件推进的浮点充能可能是0.999999；不能因此把已转好的钻头误算成一次产电连击。
        MCHSkill.钻头 => s.Level >= 58 && s.Drill >= 1 - .0001f,
        MCHSkill.空气锚 => s.Level >= 76 && s.AnchorCd <= .01f,
        MCHSkill.热弹 => s.Level >= 4 && s.Level < 76 && s.AnchorCd <= .01f,
        MCHSkill.回转飞锯 => s.Level >= 90 && s.SawCd <= .01f,
        MCHSkill.掘地飞轮 => s.Level >= 96 && s.Excavator > EffectMargin,
        _ => false
    };

    public static MachinistChoice Gcd(MachinistState s, uint reserved = 0, uint held = 0, bool planBattery = true)
    {
        var normal = MachinistFastBurst.Gcd(s, NormalGcd(s, reserved, held), held);
        return planBattery && reserved == 0 && held == 0 && normal.Action == MCHSkill.掘地飞轮
            ? MachinistBatteryPlanner.SelectGcd(s, normal) : normal;
    }

    internal static MachinistChoice NormalGcd(MachinistState s, uint reserved = 0, uint held = 0, bool planCombo = true)
    {
        MachinistModel.Advance(ref s, s.GcdLeft);
        // 整备已经交出但结果未明时保留工具，仍从其余合法GCD中选招。
        if (s.Reassemble <= 0)
        {
            switch (held)
            {
                case MCHSkill.钻头: s.Drill = 0; break;
                case MCHSkill.空气锚: case MCHSkill.热弹: s.AnchorCd = 999; break;
                case MCHSkill.回转飞锯: s.SawCd = 999; break;
                case MCHSkill.掘地飞轮: s.Excavator = 0; break;
            }
        }
        if (s.Heated && !(s.WildfireLead && !s.LeadGcdDone)) return new(s.HeatAction, "兑现过热层数");
        if (s.Reassemble > 0 && ToolReady(s, reserved)) return new(reserved, "兑现已生效整备");
        // 野火后可以同时积压旧飞轮和已转好的飞锯，必须先消耗旧预备再生成新的。
        if (ToolReady(s, MCHSkill.掘地飞轮) && ToolReady(s, MCHSkill.回转飞锯))
            return new(MCHSkill.掘地飞轮, "先兑现飞轮，避免飞锯覆盖预备");
        if (s.SawFirst && ToolReady(s, MCHSkill.回转飞锯)) return new(MCHSkill.回转飞锯, "先打飞锯QT");
        if (s.Excavator > 0 && s.Excavator <= s.Gcd + EffectMargin) return new(MCHSkill.掘地飞轮, "飞轮预备将到期");
        if (s.FullMetal > 0 && s.FullMetal <= s.Gcd + EffectMargin && s.Reassemble <= 0)
            return new(MCHSkill.全金属爆发, "全金属预备将到期");
        var anchor = s.Level >= 76 ? MCHSkill.空气锚 : MCHSkill.热弹;
        if (ToolReady(s, anchor)) return new(anchor, "空气锚及电量收入");
        if (s.Drill >= 2 - s.Gcd / 20 && ToolReady(s, MCHSkill.钻头)) return new(MCHSkill.钻头, "防钻头充能溢出");
        if (ToolReady(s, MCHSkill.回转飞锯)) return new(MCHSkill.回转飞锯, "飞锯及电量收入");
        if (ToolReady(s, MCHSkill.掘地飞轮)) return new(MCHSkill.掘地飞轮, "飞轮及电量收入");
        if (s.Reassemble > 0 && ToolReady(s, MCHSkill.钻头)) return new(MCHSkill.钻头, "整备钻头");
        if (s.HyperchargeQt && s.Heat == 45 && s.FreeHypercharge <= 0 && BurstLeft(s) > HeatCycle + s.Gcd)
            return new(Combo(s), "补5热以衔接第二次超荷");
        var preferred = s.FullMetal > EffectMargin && s.Reassemble <= 0
            ? new MachinistChoice(MCHSkill.全金属爆发, "全金属爆发")
            : ToolReady(s, MCHSkill.钻头) ? MachinistHeatPlanner.PrepareBridge(s)
                ? new(Combo(s), "团辅前续连击/补热，保留钻头衔接双过热") : new(MCHSkill.钻头, "可用钻头")
            : new(Combo(s), s.Reassemble > 0 ? "整备目标失效，正常GCD兜底" : "连击积累资源");
        if (s.ComboNext == 0 || preferred.Action == Combo(s) || s.ComboLeft <= EffectMargin) return preferred;
        // 恢复路线中的选招只保护眼前一G，不再次展开过热预测，避免相互递归。
        var resume = s.Gcd;
        if (planCombo ? MachinistHeatPlanner.ComboFirst(s, preferred.Action, out resume)
            : s.ComboLeft <= resume + EffectMargin)
            return new(Combo(s), $"真实连击将到期：剩{s.ComboLeft:F2}s，工具后预计{resume:F2}s续上");
        return planCombo ? preferred with { Reason = $"{preferred.Reason}，连击可在{resume:F2}s后续上" } : preferred;
    }

    public static uint Combo(MachinistState s)
    {
        if (s.AoeQt && s.Targets >= 3) return MCHSkill.霰弹枪;
        return s.ComboNext != 0 ? s.ComboNext : MCHSkill.热分裂弹1;
    }

    public static float BurstLeft(MachinistState s)
    {
        if (s.PartyCycle.HasValue)
        {
            var start = DamageBurstAt(s);
            return Math.Max(s.DamageWindow, start <= 0 ? start + 20 : 0);
        }
        // 实际团辅优先；没有可见团辅时，以启用的120技能建立本轮预期窗口。
        var own = s.WildfireQt && s.WildfireCd > 100 ? s.WildfireCd - 100 : 0;
        if (s.BarrelQt && s.BarrelCd > 100) own = Math.Max(own, s.BarrelCd - 100);
        return Math.Max(s.DamageWindow, own);
    }

    // 超荷关闭不等于野火/团辅的120锚点消失；只有野火关闭后才退回枪管。
    internal static float BurstAt(MachinistState s) => s.WildfireQt ? s.WildfireCd
        : s.BarrelQt ? s.BarrelCd : float.PositiveInfinity;

    internal static float DamageBurstAt(MachinistState s)
    {
        if (s.PartyCycle is not { } start || !float.IsFinite(start)) return BurstAt(s);
        if (start + 20 <= 0) start += (MathF.Floor((-start - 20) / 120) + 1) * 120;
        return start;
    }

    public static (float Start, float End) QueenWindow(MachinistState s)
    {
        if (s.DamageWindow > 0) return (0, s.DamageWindow);
        // 枪管可提前准备；不能因此把尚未到来的120窗口提前结束。
        var cd = DamageBurstAt(s);
        if (s.PartyCycle.HasValue)
            return s.WindowActive && cd >= s.WindowLeft ? (0, s.WindowLeft)
                : (Math.Max(0, cd), s.WindowActive ? Math.Min(cd + 20, s.WindowLeft) : cd + 20);
        if (cd > 100 && float.IsFinite(cd)) return (0, s.WindowActive ? Math.Min(cd - 100, s.WindowLeft) : cd - 100);
        if (s.WindowActive)
        {
            if (!float.IsFinite(cd) || cd >= s.WindowLeft) return (0, s.WindowLeft);
            return (cd, Math.Min(cd + 20, s.WindowLeft));
        }
        return (cd, cd + 20);
    }

    public static bool HeatFits(MachinistState s)
    {
        var duration = s.GcdLeft + HeatCycle;
        return s.AnchorCd >= duration - .1f && s.SawCd >= duration - .1f
            && (2 - s.Drill) * s.DrillRecast >= duration - .1f
            && s.Excavator <= 0 && s.FullMetal <= 0
            && (s.ComboNext == 0 || s.ComboLeft > duration + EffectMargin);
    }

    public static MachinistChoice Off(MachinistState s, bool queen, bool heat, bool wildfireHeat = true, bool windowHeat = false,
        bool planBattery = true)
    {
        if (Slots(s) == 0) return new(0, "没有合法能力技插槽");
        if (s.FastWildfireActive && wildfireHeat && MachinistFastBurst.CanHypercharge(s))
            return new(MCHSkill.超荷, "快速野火：接超荷，直接兑现热冲击");
        if (!s.FastWildfireActive && wildfireHeat && s.WildfireLeft > 0 && s.WildfireHits < 6 && s.CanHypercharge && (!s.WildfireLead || s.LeadGcdDone))
            return new(MCHSkill.超荷, "野火路线：接超荷");
        if (s.WildfireLeft > 0 && s.WildfireLead && !s.LeadGcdDone)
            return new(0, "野火路线：先打预定普通GCD");
        if (!s.FastBurst && s.WildfireQt && s.WildfireCd <= 0 && s.WildfireLeft <= 0 && !s.HyperchargeQt
            && (!s.WindowActive || s.WindowLeft >= 10 + EffectMargin))
            return new(MCHSkill.野火, "超荷QT关闭，按普通武器技能兑现野火");
        // 准备动作可以早于野火转好；同一G的两个位置先归完整野火，再安排其他能力技。
        if ((wildfireHeat || s.Heated || s.FastBurst) && MachinistWildfire.TryPlan(s, out var wildfire, out _))
            return wildfire.Choice(s);
        if (s.InCombat && s.BarrelQt && s.FreeHypercharge <= 0 && s.FullMetal <= 0
            && (s.BarrelCd <= 0 || wildfireHeat && MachinistWildfire.Preparing(s)
                && s.BarrelCd < s.GcdLeft - MachinistWildfire.SubmitTail(s)))
            return new(MCHSkill.枪管加热, s.BarrelCd > 0 ? "为野火保留即将转好的枪管插槽" : "枪管提供免费超荷及全金属预备",
                s.GcdTotal - s.GcdLeft + s.BarrelCd);
        // 机器人提前占一个正常插槽，不等待过热；已经承诺野火时让出超荷位置。
        if (queen && s.QueenQt && s.Battery >= 50 && s.QueenLeft <= 0 && s.QueenCd <= 0)
            return new(MCHSkill.后式自走人偶, "电量预测：召唤机器人");
        var next = Gcd(s, planBattery: planBattery).Action;
        if (s.ReassembleQt && s.ReassembleCharges >= 1 && s.Reassemble <= 0 && !s.Heated
            && s.WildfireLeft <= 0 && IsTool(next) && s.GcdLeft < 4
            && (!wildfireHeat || !MachinistWildfire.Preparing(s) || s.GcdLeft < s.WildfireCd - .1f))
            return new(MCHSkill.整备, "整备绑定下一工具");
        if (heat && s.CanHypercharge && (!s.FastWildfireActive || MachinistFastBurst.CanHypercharge(s))
            && (MachinistHeatPlanner.Fits(s) || windowHeat && s.WindowActive)) return new(MCHSkill.超荷, "热量预测：兑现过热");
        // 等第二个插槽时不让伤害能力技抢走野火/超荷的位置。
        if (wildfireHeat && MachinistWildfire.Preparing(s) && s.WildfireCd <= 0 && s.CanHypercharge && HeatFits(s))
            return new(0, "为野火路线保留插槽");
        if (s.Gauss >= 1 && (s.Gauss >= s.Ricochet || s.Ricochet < 1)) return new(MCHSkill.双将, "泄双将充能");
        if (s.Ricochet >= 1) return new(MCHSkill.将死, "泄将死充能");
        return new(0, "无可用能力技");
    }
}
