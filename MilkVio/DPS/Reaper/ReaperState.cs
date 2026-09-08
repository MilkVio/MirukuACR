using MilkVio.DPS.Reaper.ReaperData;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Data;

namespace MilkVio.DPS.Reaper;

// 一次判断使用同一份实况；这里不预测、不推进计划。
public readonly record struct ReaperState
{
    public bool IsDump => DumpQt && !WindowActive;
    public long Now { get; init; }
    public ulong PlayerId { get; init; }
    public ulong TargetId { get; init; }
    public int Level { get; init; }
    public bool Alive { get; init; }
    public bool InCombat { get; init; }
    public bool HasTarget { get; init; }
    public bool Melee { get; init; }
    public bool Moving { get; init; }
    public bool CastingCommunio { get; init; }
    public float Distance { get; init; }
    public float Gcd { get; init; }
    public float ReapGcd { get; init; }
    public float CommunioGcd { get; init; }
    public float CommunioCast { get; init; }
    public float HarpeCast { get; init; }
    public float PerfectioGcd { get; init; }
    public float HarvestMoonGcd { get; init; }
    public float GcdLeft { get; init; }
    public float GcdElapsed { get; init; }
    public uint LastGcd { get; init; }
    public int Soul { get; init; }
    public int Shroud { get; init; }
    public int Lemure { get; init; }
    public int Void { get; init; }
    public int Reavers { get; init; }
    public bool Executioner { get; init; }
    public float Enshrouded { get; init; }
    public float CircleCd { get; init; }
    public float GluttonyCd { get; init; }
    public float EnshroudCd { get; init; }
    public float SliceCharges { get; init; }
    public float SliceRecast { get; init; }
    public float CircleLeft { get; init; }
    public float DeathDesign { get; init; }
    public float ComboLeft { get; init; }
    public uint ComboNext { get; init; }
    public float Bloodsown { get; init; }
    public int SacrificeStacks { get; init; }
    public float SacrificeLeft { get; init; }
    public float FreeEnshroud { get; init; }
    public float Occulta { get; init; }
    public float Perfectio { get; init; }
    public float Oblatio { get; init; }
    public bool Soulsow { get; init; }
    public bool CircleQt { get; init; }
    public bool FastCircle { get; init; }
    public bool EnshroudQt { get; init; }
    public bool GluttonyQt { get; init; }
    public bool BloodQt { get; init; }
    public bool SliceQt { get; init; }
    public bool DotQt { get; init; }
    public bool DumpQt { get; init; }
    public bool FarPerfectioQt { get; init; }
    public bool HarvestMoonQt { get; init; }
    public bool HarpeQt { get; init; }
    public bool WindowActive { get; init; }
    public int WindowVersion { get; init; }
    public float WindowLeft { get; init; }
    public float WindowLimit { get; init; }
    public int GoalSoul { get; init; }
    public int GoalShroud { get; init; }

    public bool HasTiming => Gcd > 0 && ReapGcd > 0 && CommunioGcd > 0 && PerfectioGcd > 0;
    public bool Locked => Enshrouded > 0 || Reavers > 0;
    public bool CanEnshroud => Level >= 80 && EnshroudQt && !Locked && Perfectio <= 0
        && (!WindowActive || (WindowLeft > 0 ? WindowLeft : WindowLimit) > 0.8f)
        && EnshroudCd <= 0 && (Shroud >= 50 || FreeEnshroud > 0);
    public bool CanHarvest => Level >= 88 && !Locked && Bloodsown <= 0 && SacrificeStacks > 0
        && FreeEnshroud <= 0 && Perfectio <= 0 && Distance <= GameData.GetCurrentAttackRange(15);
    public float ShroudFinishIn => GcdLeft + Math.Max(0, Lemure - 1) * ReapGcd + CommunioCast + 0.65f;
    public bool CanHarvestMoonForRange => Alive && HasTarget && HasTiming && Level >= 90 && !CastingCommunio && Lemure > 0
        && (Lemure > 1 ? !Melee : Moving) && Reavers == 0 && HarvestMoonQt && Soulsow
        && GcdLeft <= 0.3f && HarvestMoonGcd > 0 && Distance <= GameData.GetCurrentAttackRange(25)
        && Enshrouded > ShroudFinishIn + HarvestMoonGcd
        && (Occulta <= 0 || Occulta > ShroudFinishIn + HarvestMoonGcd);
    public bool CanHarvestMoonWhileMoving => Moving && Lemure == 1 && CanHarvestMoonForRange;
    public float SingleDuration => 4 * ReapGcd + CommunioGcd + (Occulta > 0 ? PerfectioGcd : 0);
    public float DoubleDuration => 8 * ReapGcd + 2 * CommunioGcd + 2 * Gcd + PerfectioGcd;
    public float PreparationLead => 2 * Gcd + ReapGcd + Gcd / 2 + 1.2f;
    public bool ComboAtRisk(float delay) => Melee && !Locked && ComboNext != 0
        && ComboLeft > GcdLeft + 0.1f && ComboLeft <= GcdLeft + delay + 0.5f;

    public static ReaperState Read()
    {
        var me = Core.Me;
        var now = Environment.TickCount64;
        if (me == null || me.ClassJob.RowId != 39) return new ReaperState { Now = now };
        if (me.IsDead) return new ReaperState { Now = now, PlayerId = me.GameObjectId, InCombat = GameData.IsInCombat() };
        var target = Core.Target;
        var validTarget = target != null && !target.IsDead && target.IsTargetable
            && target.EntityId != me.EntityId && !target.IsPlayer();
        var distance = validTarget ? me.DistanceToMe() : float.PositiveInfinity;
        var settings = PromeSettings.Instance;
        return new ReaperState
        {
            Now = now, PlayerId = me.GameObjectId, TargetId = validTarget ? target!.GameObjectId : 0,
            Level = me.Level, Alive = true, InCombat = GameData.IsInCombat(), HasTarget = validTarget,
            Distance = distance, Melee = validTarget && distance <= GameData.GetCurrentMeleeRange(),
            Moving = MoveManager.IsLocalPlayerMoving,
            CastingCommunio = me.IsCasting && me.CastActionId == ReaperSkill.团契,
            Gcd = ReaperHelper.普通Gcd ?? 0, ReapGcd = ReaperHelper.附体Gcd ?? 0,
            CommunioGcd = ReaperHelper.读取复唱时间(ReaperSkill.团契) ?? 0,
            CommunioCast = ReaperHelper.读取咏唱时间(ReaperSkill.团契) ?? 1.3f,
            HarpeCast = ReaperHelper.读取咏唱时间(ReaperSkill.勾刃) ?? 1.3f,
            PerfectioGcd = ReaperHelper.读取复唱时间(ReaperSkill.完人) ?? 0,
            HarvestMoonGcd = ReaperHelper.读取复唱时间(ReaperSkill.收获月) ?? 0,
            GcdLeft = ActionHelper.GetGcdRemain(), GcdElapsed = ActionHelper.GetGcdElapsed(),
            LastGcd = ReaperHelper.最近公共技能(),
            Soul = JobGaugeHelper.RPR.灵魂值, Shroud = JobGaugeHelper.RPR.魂衣值,
            Lemure = JobGaugeHelper.RPR.夜游魂, Void = JobGaugeHelper.RPR.虚无魂,
            Reavers = Math.Max(ReaperHelper.IsIn妖异之镰() ? 1 : 0,
                Math.Max(me.GetStatusStackCount(ReaperBuff.妖异之镰Buff), me.GetStatusStackCount(ReaperBuff.处刑人Buff))),
            Executioner = me.HasStatus(ReaperBuff.处刑人Buff),
            Enshrouded = ReaperHelper.附体剩余时间(),
            CircleCd = ReaperSkill.神秘环.GetActionCooldown(), GluttonyCd = ReaperSkill.暴食.GetActionCooldown(),
            EnshroudCd = ReaperSkill.夜游魂衣.GetActionCooldown(),
            SliceCharges = ReaperHelper.灵魂切层数(), SliceRecast = ReaperHelper.灵魂切单层复唱(),
            CircleLeft = ReaperHelper.自身神秘环剩余(), DeathDesign = ReaperHelper.自身死亡烙印剩余(),
            ComboLeft = ActionHelper.GetComboLeftTime(), ComboNext = ReaperHelper.下一段连击(),
            Bloodsown = me.GetStatusLeftTime(ReaperBuff.死亡祭祀Buff),
            SacrificeStacks = me.GetStatusStackCount(ReaperBuff.死亡祭品Buff),
            SacrificeLeft = me.GetStatusLeftTime(ReaperBuff.死亡祭品Buff),
            FreeEnshroud = me.GetStatusLeftTime(ReaperBuff.夜游魂衣预备Buff),
            Occulta = me.GetStatusLeftTime(ReaperBuff.补完Buff), Perfectio = me.GetStatusLeftTime(ReaperBuff.完人预备Buff),
            Oblatio = me.GetStatusLeftTime(ReaperBuff.祭牲预备Buff), Soulsow = me.HasStatus(ReaperBuff.播魂种Buff),
            CircleQt = settings.GetQt(ReaperQt.神秘环), EnshroudQt = settings.GetQt(ReaperQt.附体),
            FastCircle = ReaperBattleData.Instance.FastCircle,
            GluttonyQt = settings.GetQt(ReaperQt.暴食), BloodQt = settings.GetQt(ReaperQt.隐匿挥割),
            SliceQt = settings.GetQt(ReaperQt.灵魂割), DotQt = settings.GetQt(ReaperQt.Dot),
            DumpQt = settings.GetQt(ReaperQt.倾泻资源), FarPerfectioQt = settings.GetQt(ReaperQt.远离完人),
            HarvestMoonQt = settings.GetQt(ReaperQt.收获月), HarpeQt = settings.GetQt(ReaperQt.勾刃)
        };
    }
}
