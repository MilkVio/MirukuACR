using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.DPS.Samurai.SAMData;
using GameActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using GameActionType = FFXIVClientStructs.FFXIV.Client.Game.ActionType;

namespace MilkVio.DPS.Samurai.Level100;

public static class Samurai100Helper
{
    // 仅接管百级单体。
    public static bool Enabled => Core.Me?.Level == 100 &&
        !(PromeSettings.Instance.GetQt(SAMQt.AOE) && TargetHelper.EnemyIn5m() >= 3);

    public const float EffectMargin = 0.2f;
    public static float GcdRemain => Math.Max(0, ActionHelper.GetGcdRemain());
    public static bool UseHiganbana => PromeSettings.Instance.GetQt(SAMQt.彼岸花);
    public static float HiganbanaLeft => SamuraiHelper.GetOwnHiganbanaLeftTime();

    public static unsafe float GcdSeconds
    {
        get
        {
            if (GameActionManager.Instance() == null) return 2.5f;
            var seconds = GameActionManager.GetAdjustedRecastTime(GameActionType.Action, SAMSkill.晓风) / 1000f;
            if (float.IsFinite(seconds) && seconds >= 1 && seconds <= 3) return seconds;
            var current = ActionHelper.GetGcdTotal();
            return float.IsFinite(current) && current >= 1 && current <= 3 ? current : 2.5f;
        }
    }

    public static unsafe float CastSeconds(uint actionId)
    {
        if (GameActionManager.Instance() != null)
        {
            var seconds = GameActionManager.GetAdjustedCastTime(GameActionType.Action, actionId) / 1000f;
            if (float.IsFinite(seconds) && seconds > 0 && seconds <= 3) return seconds;
        }
        return 1.3f;
    }

    public static float HiganbanaTimeAfter(int gcds)
    {
        return GcdRemain + Math.Max(0, gcds) * GcdSeconds + CastSeconds(SAMSkill.彼岸花) + EffectMargin;
    }

    public static uint GetComboId()
    {
        var time = ActionHelper.GetComboLeftTime();
        if (!float.IsFinite(time) || time <= GcdRemain + EffectMargin) return 0;
        var id = ActionHelper.GetLastComboID();
        if (id == SAMSkill.晓风 || id == SAMSkill.阵风 || id == SAMSkill.士风) return id;
        return 0;
    }

    public static int MeikyoStacks
    {
        get
        {
            var left = Core.Me.GetStatusLeftTime(SAMBuff.明镜止水);
            if (left <= GcdRemain + EffectMargin) return 0;
            var stacks = Math.Clamp(Core.Me.GetStatusStackCount(SAMBuff.明镜止水), 1, 3);
            var usable = 1 + (int)((left - GcdRemain - EffectMargin) / GcdSeconds);
            return Math.Min(stacks, usable);
        }
    }

    public static int GcdsToThreeSen()
    {
        var snow = JobGaugeHelper.SAM.HasYuki;
        var moon = JobGaugeHelper.SAM.HasMoon;
        var hana = JobGaugeHelper.SAM.HasHana;
        var count = (snow ? 0 : 2) + (moon ? 0 : 3) + (hana ? 0 : 3);
        var stacks = MeikyoStacks;
        if (stacks > 0)
        {
            if (!moon) { count -= 2; stacks--; }
            if (!hana && stacks > 0) { count -= 2; stacks--; }
            if (!snow && stacks > 0) count--;
            return count;
        }

        var combo = GetComboId();
        if (combo == SAMSkill.阵风 && !moon) return count - 2;
        if (combo == SAMSkill.士风 && !hana) return count - 2;
        if (combo == SAMSkill.晓风 && count > 0) return count - 1;
        return count;
    }

    public static int GcdsToNextHiganbana()
    {
        var count = JobGaugeHelper.SAM.GetSenCount();
        if (count == 1) return 0;
        if (count == 0)
        {
            if (MeikyoStacks > 0 || GetComboId() != 0) return 1;
            return 2;
        }
        return GcdsToHiganbanaAfterThreeSen();
    }

    private static int GcdsToHiganbanaAfterThreeSen(bool includeNewMeikyo = true)
    {
        var missing = 3 - JobGaugeHelper.SAM.GetSenCount();
        var toThree = GcdsToThreeSen();
        var returns = (PromeSettings.Instance.GetQt(SAMQt.立即回返) ? 1 : 0) + (SamuraiHelper.Has燕回返() ? 1 : 0);
        // 居合和回返也会占用明镜时间。
        var mirrorLeft = Core.Me.GetStatusLeftTime(SAMBuff.明镜止水);
        var nextSen = MeikyoStacks > missing && mirrorLeft > GcdRemain + (toThree + 1 + returns) * GcdSeconds + EffectMargin ? 1 : 2;
        var count = toThree + 1 + nextSen;
        if (includeNewMeikyo && MeikyoStacks == 0 &&
            PromeSettings.Instance.GetQt(SAMQt.明镜止水) && SamuraiHelper.明镜止水层数() >= 1 &&
            !Core.Me.HasStatus(SAMBuff.天道))
        {
            // 续完手上的连击、拿到雪后，可开明镜补月花。
            var combo = GetComboId();
            var sen = JobGaugeHelper.SAM.GetSenCount();
            var snow = JobGaugeHelper.SAM.HasYuki;
            var beforeMirror = 0;
            if ((combo == SAMSkill.阵风 && !JobGaugeHelper.SAM.HasMoon) ||
                (combo == SAMSkill.士风 && !JobGaugeHelper.SAM.HasHana))
            { beforeMirror = 1; sen++; }
            else if (combo == SAMSkill.晓风 && !snow)
            { beforeMirror = 1; sen++; snow = true; }
            else if (combo == SAMSkill.晓风 && sen < 3)
            { beforeMirror = 2; sen++; }
            if (!snow) { beforeMirror += 2; sen++; }
            if (sen <= 3)
                count = Math.Min(count, beforeMirror + (3 - sen) + 1 + 1);
        }
        return count + returns;
    }

    public static bool KeepOneSenForHiganbana()
    {
        if (!UseHiganbana || JobGaugeHelper.SAM.GetSenCount() != 1) return false;
        return HiganbanaLeft <= HiganbanaTimeAfter(GcdsToHiganbanaAfterThreeSen());
    }

    public static bool WouldDelayHiganbana(int extraGcds)
    {
        if (!UseHiganbana) return false;
        return HiganbanaLeft <= HiganbanaTimeAfter(GcdsToNextHiganbana() + extraGcds);
    }

    public static 居合类型 GetBestIaijutsu()
    {
        if (Samurai100Planning.TryGcd(out var planned, out _))
            return planned == SAMSkill.彼岸花 ? 居合类型.彼岸花 : planned == SAMSkill.纷乱雪月花 ? 居合类型.雪月花 : 居合类型.无;
        var count = JobGaugeHelper.SAM.GetSenCount();
        var moon = Core.Me.GetStatusLeftTime(SAMBuff.风月);
        var castEnd = HiganbanaTimeAfter(0);
        if (count == 3)
        {
            var tendo = Core.Me.GetStatusLeftTime(SAMBuff.天道);
            if (moon > castEnd || (tendo > castEnd && tendo <= HiganbanaTimeAfter(2)))
                return 居合类型.雪月花;
            return 居合类型.无;
        }
        if (count != 1 || !UseHiganbana || moon <= castEnd) return 居合类型.无;
        if (HiganbanaLeft <= HiganbanaTimeAfter(1)) return 居合类型.彼岸花;
        if (!KeepOneSenForHiganbana()) return 居合类型.无;

        // 明镜不能垫前段；普通连击垫完后保留收尾。
        var combo = GetComboId();
        if (MeikyoStacks > 0 ||
            (combo == SAMSkill.阵风 || combo == SAMSkill.士风) && HiganbanaLeft <= HiganbanaTimeAfter(2))
            return 居合类型.彼岸花;
        return 居合类型.无;
    }

    public static uint GetComboAction()
    {
        if (Samurai100Planning.TryGcd(out var planned, out _))
            return planned == SAMSkill.晓风 || planned == SAMSkill.阵风 || planned == SAMSkill.士风 ||
                   planned == SAMSkill.雪风 || planned == SAMSkill.月光 || planned == SAMSkill.花车 ? planned : 0;
        if (MeikyoStacks > 0) return 0;
        var combo = GetComboId();
        var count = JobGaugeHelper.SAM.GetSenCount();
        var keep = KeepOneSenForHiganbana();
        var moon = Core.Me.GetStatusLeftTime(SAMBuff.风月);
        // 风月已失效，先恢复增益，不能一直空等。
        if ((count == 3 || keep) && (combo == SAMSkill.阵风 || combo == SAMSkill.士风) &&
            moon <= HiganbanaTimeAfter(0)) return SAMSkill.晓风;
        if (combo == SAMSkill.阵风)
        {
            if (count == 3) return 0;
            if (keep) return 0;
            return JobGaugeHelper.SAM.HasMoon ? SAMSkill.晓风 : SAMSkill.月光;
        }
        if (combo == SAMSkill.士风)
        {
            if (count == 3) return 0;
            if (keep) return 0;
            return JobGaugeHelper.SAM.HasHana ? SAMSkill.晓风 : SAMSkill.花车;
        }
        if (combo != SAMSkill.晓风) return SAMSkill.晓风;

        var hana = Core.Me.GetStatusLeftTime(SAMBuff.风花);
        var refresh = HiganbanaTimeAfter(3);
        // 续花来不及走长连，现有风月够用就先取雪。
        if (count == 0 && UseHiganbana && HiganbanaLeft <= HiganbanaTimeAfter(2) && moon > HiganbanaTimeAfter(1))
            return SAMSkill.雪风;
        // 无明镜起步先补风花；居合前先补风月。
        if (moon <= 0 && hana <= 0 && count == 0) return SAMSkill.士风;
        if (moon <= refresh && (moon <= hana || count == 3 || keep)) return SAMSkill.阵风;
        if (hana <= refresh) return SAMSkill.士风;
        if (count == 3 || keep)
        {
            if (moon < hana) return SAMSkill.阵风;
            if (hana < moon) return SAMSkill.士风;
            return JobGaugeHelper.SAM.HasMoon ? SAMSkill.士风 : SAMSkill.阵风;
        }

        if (!JobGaugeHelper.SAM.HasYuki) return SAMSkill.雪风;
        var finisher = Samurai100Rules.ChooseMoonFlower(Samurai100Planning.ReadState());
        if (finisher == SAMSkill.月光) return SAMSkill.阵风;
        if (finisher == SAMSkill.花车) return SAMSkill.士风;
        return 0;
    }

    public static Combo类型 GetMeikyoType()
    {
        if (MeikyoStacks <= 0) return Combo类型.无;
        if (Samurai100Planning.TryGcd(out var planned, out _))
            return planned == SAMSkill.月光 ? Combo类型.月 : planned == SAMSkill.花车 ? Combo类型.花 : planned == SAMSkill.雪风 ? Combo类型.雪 : Combo类型.无;
        var moon = Core.Me.GetStatusLeftTime(SAMBuff.风月);
        if (JobGaugeHelper.SAM.GetSenCount() == 3)
            return moon <= HiganbanaTimeAfter(0) ? Combo类型.月 : Combo类型.无;
        if (KeepOneSenForHiganbana() && moon > HiganbanaTimeAfter(0)) return Combo类型.无;
        var finisher = Samurai100Rules.ChooseMoonFlower(Samurai100Planning.ReadState());
        if (finisher == SAMSkill.月光) return Combo类型.月;
        if (finisher == SAMSkill.花车) return Combo类型.花;
        // 已经开镜却只缺雪，恢复三闪。
        if (!JobGaugeHelper.SAM.HasYuki) return Combo类型.雪;
        return Combo类型.无;
    }

    public static bool ShouldUseMeikyo(out string reason)
    {
        reason = "保留明镜";
        if (!PromeSettings.Instance.GetQt(SAMQt.明镜止水)) { reason = "未开启明镜QT"; return false; }
        var charge = SamuraiHelper.明镜止水层数();
        if (charge < 1 || Core.Me.HasStatus(SAMBuff.明镜止水)) return false;
        if (Core.Me.HasStatus(SAMBuff.天道)) { reason = "先兑换已有天道"; return false; }
        if (GetComboId() != 0) { reason = "先续完连击"; return false; }
        if (Core.Me.DistanceToMe() > GameData.GetCurrentMeleeRange()) return false;
        if (KeepOneSenForHiganbana()) { reason = "先用已有一闪续花"; return false; }
        if (!Samurai100Projection.CanMirror(Samurai100Planning.ReadState()))
        { reason = "三层明镜无法用月花正常衔接，先调整资源"; return false; }
        if (Samurai100Planning.TryOff(SAMSkill.明镜止水, out var planned, out reason)) return planned;

        var count = JobGaugeHelper.SAM.GetSenCount();
        var needBuff = Core.Me.GetStatusLeftTime(SAMBuff.风月) <= HiganbanaTimeAfter(1) ||
                       Core.Me.GetStatusLeftTime(SAMBuff.风花) <= GcdRemain + GcdSeconds;
        if (needBuff && count != 3) { reason = "明镜恢复增益"; return true; }
        var prepareFlower = UseHiganbana &&
                            HiganbanaLeft <= HiganbanaTimeAfter(GcdsToHiganbanaAfterThreeSen(false));
        var fullSoon = (2 - charge) * 55 <= GcdsToThreeSen() * GcdSeconds + 3 * GcdSeconds;
        var burst = Samurai100Burst.HasDamageWindow || PromeSettings.Instance.GetQt(SAMQt.倾泻资源);
        if (count == 0 && UseHiganbana && HiganbanaLeft <= HiganbanaTimeAfter(2))
        { reason = "明镜取一闪续花"; return true; }
        if (count < 3 && Samurai100Burst.NeedSpaceWithOneWeave())
        { reason = "只有一个插入位，先泄剑气再开镜"; return false; }
        if (count == 3 && (prepareFlower || PromeSettings.Instance.GetQt(SAMQt.倾泻资源)))
        {
            reason = "明镜准备天道居合";
            return true;
        }
        if (count < 3 && JobGaugeHelper.SAM.HasYuki && (prepareFlower || fullSoon || burst))
        {
            reason = "已有雪，明镜补月花";
            return true;
        }
        return false;
    }

    public static string GetWaitReason()
    {
        if (Core.Me.HasStatus(SAMBuff.明镜止水)) return "等居合消耗闪，保留明镜";
        if (KeepOneSenForHiganbana()) return "垫刀已完成，等彼岸花读条";
        return "三闪垫刀已完成，等居合读条";
    }

    public static uint WaitingIaijutsu()
    {
        var s = Samurai100Planning.ReadState();
        var keep = KeepOneSenForHiganbana();
        if (Samurai100Planning.TryGcd(out var action, out _))
        {
            if (action != 0) return 0;
            keep = Samurai100Projection.KeepSen(s);
        }
        return Samurai100Rules.WaitingIaijutsu(s, keep);
    }
}
