using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Planning;

internal static class MachinistModel
{
    public static void Advance(ref MachinistState s, float seconds)
    {
        if (seconds <= 0) return;
        static float Left(float value, float dt) => Math.Max(0, value - dt);
        s.GcdLeft = Left(s.GcdLeft, seconds); s.Lock = Left(s.Lock, seconds);
        s.AnchorCd = Left(s.AnchorCd, seconds); s.SawCd = Left(s.SawCd, seconds);
        s.WildfireCd = Left(s.WildfireCd, seconds); s.BarrelCd = Left(s.BarrelCd, seconds);
        s.HyperchargeCd = Left(s.HyperchargeCd, seconds);
        s.HyperchargeRecoveryLeft = Left(s.HyperchargeRecoveryLeft, seconds);
        s.Drill = Math.Min(s.Level >= 94 ? 2 : 1, s.Drill + seconds / Math.Max(1, s.DrillRecast));
        s.ReassembleCharges = Math.Min(s.Level >= 84 ? 2 : 1, s.ReassembleCharges + seconds / 55);
        s.Gauss = Math.Min(3, s.Gauss + seconds / 30); s.Ricochet = Math.Min(3, s.Ricochet + seconds / 30);
        s.Reassemble = Left(s.Reassemble, seconds); s.FreeHypercharge = Left(s.FreeHypercharge, seconds);
        s.Excavator = Left(s.Excavator, seconds); s.FullMetal = Left(s.FullMetal, seconds);
        s.Overheat = Left(s.Overheat, seconds); if (s.Overheat <= 0) s.OverheatStacks = 0;
        s.WildfireLeft = Left(s.WildfireLeft, seconds);
        if (s.WildfireLeft <= 0) { s.WildfireLead = false; s.LeadGcdDone = false; s.FastWildfire = false; s.WildfireRecovery = false; }
        s.QueenLeft = Left(s.QueenLeft, seconds);
        s.QueenCd = Left(s.QueenCd, seconds);
        s.PartyLeft = Left(s.PartyLeft, seconds); s.PotionLeft = Left(s.PotionLeft, seconds);
        if (s.PartyCycle.HasValue) s.PartyCycle -= seconds;
        s.ComboLeft = Left(s.ComboLeft, seconds); if (s.ComboLeft <= 0) s.Combo = 0;
        s.WindowLeft = Left(s.WindowLeft, seconds); s.WindowLimit = Left(s.WindowLimit, seconds);
        s.ReserveLeft = Left(s.ReserveLeft, seconds);
    }

    public static int BatteryGain(uint action) => action switch
    {
        MCHSkill.空气锚 or MCHSkill.热弹 or MCHSkill.回转飞锯 or MCHSkill.掘地飞轮 => 20,
        MCHSkill.热狙击弹3 or MCHSkill.狙击弹3 => 10,
        _ => 0
    };

    public static int HeatGain(uint action) => action switch
    {
        MCHSkill.霰弹枪 => 10,
        MCHSkill.热分裂弹1 or MCHSkill.热独头弹2 or MCHSkill.热狙击弹3
            or MCHSkill.分裂弹1 or MCHSkill.独头弹2 or MCHSkill.狙击弹3 => 5,
        _ => 0
    };

    public static void Apply(ref MachinistState s, uint action, bool recoveryWildfire = false)
    {
        if (MachinistRules.IsWeaponskill(action))
        {
            if (s.WildfireLeft > MachinistRules.EffectMargin) s.WildfireHits = Math.Min(6, s.WildfireHits + 1);
            if (s.WildfireLead && !MachinistRules.IsHeatShot(action)) s.LeadGcdDone = true;
            s.Battery = Math.Min(100, s.Battery + BatteryGain(action));
            s.Heat = Math.Min(100, s.Heat + HeatGain(action));
            s.GcdTotal = MachinistRules.IsHeatShot(action) ? 1.5f : s.Gcd;
            s = s with { MaxWeaves = s.NormalMaxWeaves > 0 ? s.NormalMaxWeaves : 2 };
            s.GcdLeft = s.GcdTotal; s.Weaves = 0;
            if (action != MCHSkill.全金属爆发) s.Reassemble = 0;
        }
        else s.Weaves++;
        s.Lock = s.ActionLock > 0 ? Math.Clamp(s.ActionLock, .3f, .8f) : Math.Max(.3f, s.WeaveLock);
        switch (action)
        {
            case MCHSkill.钻头: s.Drill = Math.Max(0, s.Drill - 1); break;
            case MCHSkill.空气锚: case MCHSkill.热弹: s.AnchorCd = s.AnchorRecast; break;
            case MCHSkill.回转飞锯: s.SawCd = s.SawRecast; s.Excavator = 30; break;
            case MCHSkill.掘地飞轮: s.Excavator = 0; break;
            case MCHSkill.全金属爆发: s.FullMetal = 0; break;
            case MCHSkill.热分裂弹1: case MCHSkill.分裂弹1:
            case MCHSkill.热独头弹2: case MCHSkill.独头弹2: s.Combo = action; s.ComboLeft = 30; break;
            case MCHSkill.热狙击弹3: case MCHSkill.狙击弹3: s.Combo = 0; s.ComboLeft = 0; break;
            case MCHSkill.超荷:
                if (s.FreeHypercharge > 0) s.FreeHypercharge = 0; else s.Heat = Math.Max(0, s.Heat - 50);
                s.HyperchargeCd = 10; s.Overheat = 10; s.OverheatStacks = 5;
                s.HyperchargeRecoveryLeft = s.WildfireCd <= 10 + .001f ? MachinistWildfire.RecoveryWindow(s.Gcd) : 0; break;
            case MCHSkill.热冲击: case MCHSkill.烈焰弹: case MCHSkill.自动弩:
                s.OverheatStacks = Math.Max(0, s.OverheatStacks - 1);
                if (s.OverheatStacks == 0) s.Overheat = 0;
                if (action != MCHSkill.自动弩)
                { s.Gauss = Math.Min(3, s.Gauss + .5f); s.Ricochet = Math.Min(3, s.Ricochet + .5f); }
                break;
            case MCHSkill.枪管加热: s.BarrelCd = 120; s.FreeHypercharge = 30; s.FullMetal = 30; break;
            case MCHSkill.后式自走人偶: s.Battery = 0; s.QueenLeft = MachinistQueen.Duration; s.QueenCd = 6; break;
            case MCHSkill.野火:
                s.WildfireCd = 120; s.WildfireLeft = 10; s.WildfireHits = 0;
                s.FastWildfire = s.FastBurst;
                s.WildfireRecovery = recoveryWildfire;
                s.HyperchargeRecoveryLeft = 0;
                s.WildfireLead = !s.FastWildfire && !s.WildfireRecovery && !s.Heated && s.HyperchargeQt;
                s.LeadGcdDone = false; break;
            case MCHSkill.整备: s.ReassembleCharges = Math.Max(0, s.ReassembleCharges - 1); s.Reassemble = 5; break;
            case MCHSkill.双将: s.Gauss = Math.Max(0, s.Gauss - 1); break;
            case MCHSkill.将死: s.Ricochet = Math.Max(0, s.Ricochet - 1); break;
        }
    }
}
