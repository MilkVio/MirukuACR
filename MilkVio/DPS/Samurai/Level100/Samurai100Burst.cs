using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Data;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Level100;

public static class Samurai100Burst
{
    private static readonly uint[] PartyBuffs = { 1822, 1848, 1825, 141, 2964, 2599, 1297,
        2703, 3685, 1182, 1185, 786, 1878, 3887 };

    public static float PotionLeft => Core.Me.GetStatusLeftTime(49);
    public static float PartyBuffLeft => PartyBuffs.Max(id => Core.Me.GetStatusLeftTime(id));
    public static bool HasDamageWindow => Math.Max(PotionLeft, PartyBuffLeft) > Samurai100Helper.GcdRemain;

    public static bool ShouldUseOgi(out string reason)
    {
        if (Samurai100Planning.TryGcd(out var planned, out reason)) return planned == SAMSkill.奥义斩浪;
        reason = "等待奥义预备";
        var left = Core.Me.GetStatusLeftTime(SAMBuff.奥义浪斩预备);
        if (left <= 0) return false;
        var state = Samurai100Planning.ReadState();
        if (state.Distance <= state.IaiRange && Samurai100Rules.TryTsubameControl(state, out var useReturn, out var returnReason) && useReturn)
        { reason = returnReason; return false; }
        if (Samurai100Weave.BeforeCast(Samurai100Projection.AtNextGcd(Samurai100Planning.ReadState()), SAMSkill.奥义斩浪,
            Samurai100Planning.PotionPending) != SAMSkill.奥义斩浪)
        { reason = "先垫刀给照破或到期能力技留位置"; return false; }
        var gcd = Samurai100Helper.GcdSeconds;
        if (left <= Samurai100Helper.HiganbanaTimeAfter(2)) { reason = "奥义预备将到期"; return true; }
        if (Samurai100Helper.WouldDelayHiganbana(2)) { reason = "先安排彼岸花"; return false; }
        if (SamuraiHelper.Has燕回返() && (state.Immediate ||
            SamuraiHelper.燕回返LeftTime() <= Samurai100Helper.GcdRemain + 3 * gcd))
        { reason = "先兑现回返"; return false; }
        var meikyo = Core.Me.GetStatusLeftTime(SAMBuff.明镜止水);
        if (meikyo > 0 && meikyo <= Samurai100Helper.GcdRemain + (Samurai100Helper.MeikyoStacks + 2) * gcd)
        { reason = "先用完明镜"; return false; }
        if (Core.Me.HasStatus(SAMBuff.天道) && JobGaugeHelper.SAM.GetSenCount() >= 2)
        { reason = "先衔接天道居合"; return false; }
        if (!Core.Me.HasStatus(SAMBuff.风月) || !Core.Me.HasStatus(SAMBuff.风花))
        { reason = "先补自身增益"; return false; }
        reason = PotionLeft > 0 ? "药内奥义" : "兑现奥义预备";
        return true;
    }

    public static bool ShouldUseTsubame(out string reason)
    {
        reason = "保留回返";
        if (!SamuraiHelper.Has燕回返()) return false;
        if (Samurai100Rules.TryTsubameControl(Samurai100Planning.ReadState(), out var controlled, out var controlReason))
        { reason = controlReason; return controlled; }
        if (PromeSettings.Instance.GetQt(SAMQt.立即回返)) { reason = "立即回返"; return true; }
        if (Samurai100Planning.TryGcd(out var planned, out reason)) return planned == SAMSkill.燕回返;
        var left = SamuraiHelper.燕回返LeftTime();
        if (left <= Samurai100Helper.GcdRemain + Samurai100Helper.GcdSeconds + Samurai100Helper.EffectMargin)
        { reason = "回返即将到期"; return true; }
        if (JobGaugeHelper.SAM.GetSenCount() == 3)
        { reason = "新居合前先用旧回返"; return true; }
        if (!Samurai100Rules.ReturnBeforeFlower(Samurai100Planning.ReadState()))
        { reason = "先取闪续花"; return false; }
        if (Samurai100Rules.FinishingReturn(Samurai100Planning.ReadState()))
        { reason = "完成本轮刚生成的回返"; return true; }
        if (Core.Me.DistanceToMe() > GameData.GetCurrentMeleeRange())
        { reason = "远离使用回返"; return true; }
        var combo = Samurai100Helper.GetComboId();
        if (Samurai100Helper.KeepOneSenForHiganbana() &&
            (combo == SAMSkill.阵风 || combo == SAMSkill.士风))
        { reason = "回返垫刀，保留一闪续花"; return true; }
        if (HasDamageWindow || Samurai100Rules.Spending(Samurai100Planning.ReadState()) || PromeSettings.Instance.GetQt(SAMQt.倾泻资源))
        { reason = PotionLeft > 0 ? "药内回返" : "当前爆发及时兑现回返"; return true; }
        if (MoveManager.IsLocalPlayerMoving && Samurai100Helper.GetComboAction() == 0)
        { reason = "移动时使用回返"; return true; }

        var nextBurst = float.MaxValue;
        if (SamuraiHelper.AllowIkishoten) nextBurst = SAMSkill.意气冲天.GetActionCooldown();
        if (SamuraiHelper.AllowSenei) nextBurst = Math.Min(nextBurst, SAMSkill.必杀剑_闪影.GetActionCooldown());
        if (nextBurst > left - Samurai100Helper.GcdSeconds)
        { reason = "期限内没有自身爆发，使用回返"; return true; }
        return false;
    }

    public static bool ShouldUseZanshin(out string reason)
    {
        if (Samurai100Planning.TryOff(SAMSkill.残心, out var planned, out reason)) return planned;
        reason = "等待残心";
        var state = Samurai100Planning.ReadState();
        if (Samurai100Weave.SeneiReady(state)) { reason = "先使用已就绪的闪影"; return false; }
        if (!Samurai100Rules.CanZanshin(state)) { reason = "为闪影保留25剑气"; return false; }
        if (Core.Me.DistanceToMe() > GameData.GetCurrentAttackRange(8)) return false;
        if (Samurai100Weave.ShohaReady(state)) { reason = "先用照破，防止剑压溢出"; return false; }
        var left = Core.Me.GetStatusLeftTime(SAMBuff.残心预备);
        if (left <= Samurai100Helper.GcdRemain + Samurai100Helper.GcdSeconds)
        { reason = "残心即将到期"; return true; }
        if (!Core.Me.HasStatus(SAMBuff.风月)) { reason = "先补风月"; return false; }
        reason = PotionLeft > 0 ? "药内残心" : "兑现残心预备";
        return true;
    }

    public static int NextKenkiGain()
    {
        return Samurai100Rules.NextGain(Samurai100Planning.ReadBudgetState());
    }

    public static bool ShouldUseIkishoten(out string reason)
    {
        if (Samurai100Planning.TryOff(SAMSkill.意气冲天, out var planned, out reason)) return planned;
        return Samurai100Rules.CanIkishoten(Samurai100Planning.ReadState(), out reason);
    }

    private static bool CanUseShinten()
    {
        return SamuraiHelper.AllowShinten && JobGaugeHelper.SAM.剑气 >= 25 && SAMSkill.必杀剑_震天.GetActionCooldown() <= 0 &&
               Core.Me.DistanceToMe() <= GameData.GetCurrentMeleeRange();
    }

    public static int ReservedKenki()
    {
        return Samurai100Rules.Reserve(Samurai100Planning.ReadBudgetState());
    }

    public static bool NeedSpaceWithOneWeave()
    {
        return Samurai100Planning.MaxWeaves == 1 && JobGaugeHelper.SAM.剑气 > 85 && CanUseShinten();
    }

    public static bool ShouldUseShinten(out string reason)
    {
        if (Samurai100Planning.TryOff(SAMSkill.必杀剑_震天, out var planned, out reason)) return planned;
        return Samurai100Rules.SpendKenki(Samurai100Planning.ReadState(), out reason);
    }
}
