using System.Numerics;
using System.Reflection;
using Dalamud.Game.ClientState.Objects.Types;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeGameData = PromeRotation.Core.GameData;

namespace MilkVio.Healer.Common;

public static class HealerUtils
{
    private static readonly ActionTargetType[] PartyTargets =
    {
        ActionTargetType.Self,
        ActionTargetType.PartyMember2,
        ActionTargetType.PartyMember3,
        ActionTargetType.PartyMember4,
        ActionTargetType.PartyMember5,
        ActionTargetType.PartyMember6,
        ActionTargetType.PartyMember7,
        ActionTargetType.PartyMember8,
    };

    private static readonly uint[] AstMeleeCardPriority =
    [
        34, // SAM
        30, // NIN
        22, // DRG
        20, // MNK
        39, // RPR
        41, // VPR
        32, // DRK
        37, // GNB
        21, // WAR
        19, // PLD
        33, // AST
        42, // PCT
        31, // MCH
        27, // SMN
        35, // RDM
        25, // BLM
        38, // DNC
        23, // BRD
        24, // WHM
        40, // SGE
        28, // SCH
    ];

    private static readonly uint[] AstRangedCardPriority =
    [
        42, // PCT
        31, // MCH
        27, // SMN
        35, // RDM
        25, // BLM
        38, // DNC
        23, // BRD
        24, // WHM
        40, // SGE
        28, // SCH
        33, // AST
        34, // SAM
        30, // NIN
        22, // DRG
        20, // MNK
        39, // RPR
        41, // VPR
        32, // DRK
        37, // GNB
        21, // WAR
        19, // PLD
    ];

    private static DateTime? BattleStartedAtUtc { get; set; }

    private static DateTime? LastSchDissipationSeenAtUtc { get; set; }

    public static IBattleChara? Me => Core.Me;

    public static IBattleChara? Target => Core.Target;

    public static byte Level => Me?.Level ?? 0;

    public static bool Qt(string key) => PromeSettings.Instance.GetQt(key);

    public static void MarkBattleStarted()
    {
        BattleStartedAtUtc = DateTime.UtcNow;
    }

    public static void MarkBattleEnded()
    {
        BattleStartedAtUtc = null;
        LastSchDissipationSeenAtUtc = null;
    }

    public static double BattleTimeSeconds
        => BattleStartedAtUtc is { } startedAt ? Math.Max(0, (DateTime.UtcNow - startedAt).TotalSeconds) : 0;

    public static bool BattleTimeAtLeast(double seconds)
        => BattleStartedAtUtc == null || BattleTimeSeconds >= seconds;

    public static CheckResult Fail(string message) => new(false, message);

    public static CheckResult Pass(string message) => new(true, message);

    public static CheckResult RequireEnemyTarget(float range = 25f, uint minMp = 0, bool allowMoving = true)
    {
        if (Qt(HealerQt.停手)) return Fail("停手QT已开启");

        var me = Me;
        if (me == null) return Fail("自身未加载");
        if (minMp > 0 && me.CurrentMp < minMp) return Fail("魔力不足");

        var target = Target;
        if (target == null) return Fail("当前无目标");
        if (target.EntityId == me.EntityId) return Fail("当前目标为自己");
        if (target.IsPlayer()) return Fail("当前目标为玩家");
        if (target.CurrentHp == 0) return Fail("目标已死亡");

        var currentAttackRange = PromeGameData.GetCurrentAttackRange(range);
        var targetDistance = Vector3.Distance(me.Position, target.Position) - (me.HitboxRadius + target.HitboxRadius);
        if (targetDistance > currentAttackRange) return Fail($"当前目标过远（>{currentAttackRange:0.#}m）");

        if (!allowMoving && IsMovingWithoutInstant()) return Fail("当前正在移动");

        return Pass("目标可用");
    }

    public static bool IsMovingWithoutInstant()
    {
        var me = Me;
        if (me == null) return true;
        return MoveManager.IsLocalPlayerMoving && !me.HasStatus(RoleBuff.即刻咏唱) && !me.HasStatus(AstBuff.光速);
    }

    public static bool CanHardCast() => !IsMovingWithoutInstant();

    public static bool IsReady(uint actionId, byte minLevel = 0)
    {
        if (Level < minLevel) return false;
        var adjusted = Adjust(actionId);
        return adjusted != 0 && adjusted.GetActionCooldown() <= 0.05f;
    }

    public static float ActionCooldown(uint actionId, byte minLevel = 0)
    {
        if (Level < minLevel) return float.MaxValue;
        var adjusted = Adjust(actionId);
        return adjusted == 0 ? float.MaxValue : adjusted.GetActionCooldown();
    }

    public static bool HasCharge(uint actionId, byte minLevel = 0)
    {
        if (Level < minLevel) return false;
        var adjusted = Adjust(actionId);
        return adjusted != 0 && adjusted.GetActionCharges() >= 1f;
    }

    public static uint Adjust(uint actionId)
    {
        var adjusted = actionId.GetAdjustedActionId();
        return adjusted == 0 ? actionId : adjusted;
    }

    public static PAction Gcd(uint actionId, ActionTargetType target = ActionTargetType.Target)
        => new(Adjust(actionId), ActionType.Gcd, target);

    public static PAction OffGcd(uint actionId, ActionTargetType target = ActionTargetType.Target)
        => new(Adjust(actionId), ActionType.OffGcd, target);

    public static bool ShouldRefreshAnyStatus(IBattleChara target, float refreshAt, params uint[] statuses)
    {
        var left = statuses
            .Where(target.HasStatus)
            .Select(target.GetStatusLeftTime)
            .DefaultIfEmpty(0f)
            .Min();

        return left <= refreshAt;
    }

    public static bool ShouldRefreshAnyStatusBelow(IBattleChara target, float refreshAt, params uint[] statuses)
    {
        var left = statuses
            .Where(target.HasStatus)
            .Select(target.GetStatusLeftTime)
            .DefaultIfEmpty(0f)
            .Min();

        return left < refreshAt;
    }

    public static bool ShouldRefreshAnyStatusBelowFromSource(IBattleChara target, uint sourceId, float refreshAt, params uint[] statuses)
    {
        var left = target.StatusList
            .Where(status => statuses.Contains(status.StatusId) && status.SourceId == sourceId)
            .Select(status => status.RemainingTime)
            .DefaultIfEmpty(0f)
            .Min();

        return left < refreshAt;
    }

    public static bool ShouldRefreshAnyStatusFromSource(IBattleChara target, uint sourceId, float refreshAt, params uint[] statuses)
    {
        var left = target.StatusList
            .Where(status => statuses.Contains(status.StatusId) && status.SourceId == sourceId)
            .Select(status => status.RemainingTime)
            .DefaultIfEmpty(0f)
            .Min();

        return left <= refreshAt;
    }

    private static float StatusLeftFromSource(IBattleChara target, uint sourceId, params uint[] statuses)
    {
        return target.StatusList
            .Where(status => statuses.Contains(status.StatusId) && status.SourceId == sourceId)
            .Select(status => status.RemainingTime)
            .DefaultIfEmpty(0f)
            .Min();
    }

    public static bool TargetHasAnyStatus(IBattleChara target, params uint[] statuses)
        => statuses.Any(target.HasStatus);

    public static bool ShouldRefreshTargetStatusBelow(float refreshAt, params uint[] statuses)
    {
        var target = Target;
        var me = Me;
        if (target == null || me == null) return false;
        if (!Qt(HealerQt.DOT)) return false;
        if (target.CurrentHp == 0 || target.MaxHp == 0) return false;
        if (HpPercent(target) < 8f) return false;

        return ShouldRefreshAnyStatusBelow(target, refreshAt, statuses);
    }

    public static bool ShouldRefreshOwnTargetStatusBelow(float refreshAt, params uint[] statuses)
    {
        var target = Target;
        var me = Me;
        if (target == null || me == null) return false;
        if (!Qt(HealerQt.DOT)) return false;
        if (target.CurrentHp == 0 || target.MaxHp == 0) return false;
        if (HpPercent(target) < 8f) return false;

        return ShouldRefreshAnyStatusBelowFromSource(target, me.EntityId, refreshAt, statuses);
    }

    public static bool ShouldDot(params uint[] statuses)
    {
        var target = Target;
        var me = Me;
        if (target == null || me == null) return false;
        if (!Qt(HealerQt.DOT)) return false;
        if (target.CurrentHp == 0 || target.MaxHp == 0) return false;
        if (HpPercent(target) < 8f) return false;
        if (Qt(HealerQt.AOE) && EnemyCountAroundTarget(5f) >= 3) return false;

        return ShouldRefreshAnyStatusFromSource(target, me.EntityId, 3f, statuses);
    }

    public static bool ShouldAstDot(float refreshAt, params uint[] statuses)
        => CheckAstDot(refreshAt, statuses).Success;

    public static CheckResult CheckAstDot(float refreshAt, params uint[] statuses)
    {
        var target = Target;
        var me = Me;
        if (target == null || me == null) return Fail("目标或自身未加载");
        if (!Qt(HealerQt.DOT)) return Fail("DOT QT关闭");
        if (target.CurrentHp == 0 || target.MaxHp == 0) return Fail("目标不可用");
        if (HpPercent(target) < 3f) return Fail("目标血量过低");
        if (Qt(HealerQt.AOE) && EnemyCountAroundTarget(5f) >= 3) return Fail("AOE场景跳过单体DOT");

        var left = StatusLeftFromSource(target, me.EntityId, statuses);
        if (left <= 0f) return Pass("目标无DOT");
        if (left <= refreshAt) return Pass("DOT即将到期");

        return Fail($"DOT时间充足:{left:0.#}s");
    }

    public static bool ShouldSchDot(float refreshAt, params uint[] statuses)
    {
        var target = Target;
        var me = Me;
        if (target == null || me == null) return false;
        if (!Qt(HealerQt.DOT)) return false;
        if (target.CurrentHp == 0 || target.MaxHp == 0) return false;
        if (HpPercent(target) < 2f) return false;
        if (Qt(HealerQt.AOE) && EnemyCountAroundSelf(5f) >= 2) return false;

        return ShouldRefreshAnyStatusFromSource(target, me.EntityId, refreshAt, statuses);
    }

    public static uint EnemyCountAroundSelf(float range)
    {
        try
        {
            return TargetHelper.EnemyInRange(range);
        }
        catch
        {
            return 0;
        }
    }

    public static uint EnemyCountAroundTarget(float range)
    {
        try
        {
            var target = Target;
            return target == null ? 0 : TargetHelper.EnemyInRangeTarget(target, range);
        }
        catch
        {
            return 0;
        }
    }

    public static float HpPercent(IBattleChara chara)
    {
        if (chara.MaxHp == 0) return 100f;
        return chara.CurrentHp * 100f / chara.MaxHp;
    }

    public static IReadOnlyList<IBattleChara> Party()
    {
        try
        {
            var party = PartyHelper.GetParty();
            if (party.Count > 0) return party;
        }
        catch
        {
            // ignored, fallback below
        }

        return Me is { } me ? new[] { me } : Array.Empty<IBattleChara>();
    }

    public static int LowPartyCount(float threshold)
        => Party().Count(p => p.CurrentHp > 0 && HpPercent(p) <= threshold);

    public static bool TryGetLowestParty(float threshold, out ActionTargetType targetType)
    {
        var target = Party()
            .Where(p => p.CurrentHp > 0 && HpPercent(p) <= threshold)
            .OrderBy(HpPercent)
            .FirstOrDefault();

        targetType = ToPartyTargetType(target);
        return target != null;
    }

    public static bool TryGetPartyBelowAverage(float maxHpPercent, float gapPercent, out ActionTargetType targetType)
    {
        var party = Party()
            .Where(p => p.CurrentHp > 0)
            .ToArray();

        if (party.Length == 0)
        {
            targetType = ActionTargetType.Self;
            return false;
        }

        var average = party.Average(HpPercent);
        var target = party
            .Where(p => HpPercent(p) <= maxHpPercent)
            .Where(p => average - HpPercent(p) >= gapPercent)
            .OrderBy(HpPercent)
            .FirstOrDefault();

        targetType = ToPartyTargetType(target);
        return target != null;
    }

    public static bool TryGetLowestTank(float threshold, out ActionTargetType targetType)
    {
        var target = Party()
            .Where(p => p.CurrentHp > 0 && IsTank(p) && HpPercent(p) <= threshold)
            .OrderBy(HpPercent)
            .FirstOrDefault();

        targetType = ToPartyTargetType(target);
        return target != null;
    }

    public static bool TryGetDeadParty(out ActionTargetType targetType)
    {
        var target = Party().FirstOrDefault(p => p.CurrentHp == 0);
        targetType = ToPartyTargetType(target);
        return target != null;
    }

    public static bool TryGetCleanseTarget(out ActionTargetType targetType)
    {
        var target = Party()
            .Where(p => p.CurrentHp > 0)
            .FirstOrDefault(HasDispellableStatus);

        targetType = ToPartyTargetType(target);
        return target != null;
    }

    public static bool TryGetScholarCleanseTarget(bool h1Priority, out ActionTargetType targetType)
    {
        var party = Party()
            .Where(p => p.CurrentHp > 0)
            .ToArray();

        var me = Me;
        var tanks = party.Where(p => me == null || p.EntityId != me.EntityId).Where(IsTank).ToArray();
        var healers = party.Where(p => me == null || p.EntityId != me.EntityId).Where(IsHealer).ToArray();
        var dps = party.Where(p => me == null || p.EntityId != me.EntityId).Where(p => !IsTank(p) && !IsHealer(p)).ToArray();

        var ordered = h1Priority
            ? new[] { me }.Where(p => p != null).Cast<IBattleChara>()
                .Concat(tanks)
                .Concat(dps)
                .Concat(healers.Reverse())
            : new[] { me }.Where(p => p != null).Cast<IBattleChara>()
                .Concat(dps.Reverse())
                .Concat(tanks.Reverse())
                .Concat(healers);

        var target = ordered.FirstOrDefault(HasDispellableStatus);
        targetType = ToPartyTargetType(target);
        return target != null;
    }

    private static bool HasDispellableStatus(IBattleChara chara)
    {
        try
        {
            foreach (var status in chara.StatusList)
            {
                if (status.StatusId == 0 || status.RemainingTime <= 0)
                    continue;

                try
                {
                    if (status.GameData.Value.CanDispel)
                        return true;
                }
                catch
                {
                    // Ignore statuses whose sheet row is unavailable.
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public static ActionTargetType ToPartyTargetType(IBattleChara? chara)
    {
        if (chara == null) return ActionTargetType.Self;

        var me = Me;
        if (me != null && chara.EntityId == me.EntityId) return ActionTargetType.Self;

        var party = Party();
        for (var i = 0; i < party.Count && i < PartyTargets.Length; i++)
        {
            if (party[i].EntityId == chara.EntityId) return PartyTargets[i];
        }

        return ActionTargetType.Self;
    }

    public static bool IsTank(IBattleChara chara)
    {
        var job = chara.ClassJob.RowId;
        return job is 1 or 3 or 19 or 21 or 32 or 37;
    }

    public static bool IsHealer(IBattleChara chara)
    {
        var job = chara.ClassJob.RowId;
        return job is 6 or 24 or 28 or 33 or 40;
    }

    public static bool IsMeleeCardTarget(IBattleChara chara)
    {
        var job = chara.ClassJob.RowId;
        return job is 1 or 2 or 3 or 4 or 19 or 20 or 21 or 22 or 30 or 32 or 34 or 37 or 39 or 41;
    }

    public static bool IsAstDamageCard(string card)
        => card is "Balance" or "Spear";

    public static bool IsAstMeleeDamageCard(string card)
        => card == "Balance";

    public static ActionTargetType BestAstCardTarget(string card)
    {
        // Balance 使用近战优先表，Spear 使用远程优先表；同优先级时尽量不优先发给自己。
        var priority = IsAstMeleeDamageCard(card) ? AstMeleeCardPriority : AstRangedCardPriority;
        var me = Me;
        var party = Party()
            .Where(p => p.CurrentHp > 0)
            .OrderBy(p => AstCardPriorityIndex(priority, p.ClassJob.RowId))
            .ThenBy(p => p.EntityId == me?.EntityId)
            .FirstOrDefault();

        return ToPartyTargetType(party);
    }

    private static int AstCardPriorityIndex(uint[] priority, uint jobId)
    {
        var index = Array.IndexOf(priority, jobId);
        return index < 0 ? int.MaxValue : index;
    }

    public static bool TrySchAetherflow(out byte value)
    {
        value = 0;

        if (TryReadPromeGauge<short>("SCH", "AetherflowStack", out var stacks))
        {
            value = (byte)Math.Clamp((int)stacks, 0, 3);
            return true;
        }

        return TryReadPromeGauge("SCH", "Aetherflow", out value);
    }

    public static byte SchAetherflow()
        => TrySchAetherflow(out var value) ? value : (byte)0;

    public static byte SchFairyGauge()
        => TryReadPromeGauge<byte>("SCH", "FairyGauge", out var value) ? value : (byte)0;

    public static short SchSeraphTimer()
        => TryReadPromeGauge<short>("SCH", "SeraphTimer", out var value) ? value : (short)0;

    public static bool? SchHasPet()
    {
        if (TryReadPromeGauge<bool>("SCH", "HasPet", out var hasPet))
            return hasPet;

        // Prome's SMN.HasPet is a generic PetBuddy check, so it also reflects SCH's fairy.
        if (TryReadPromeGauge<bool>("SMN", "HasPet", out var genericHasPet))
            return genericHasPet;

        return null;
    }

    public static string SchDismissedFairyText()
        => TryReadPromeGaugeValue("SCH", "DismissedFairy", out var value)
            ? value?.ToString() ?? "None"
            : "Unknown";

    public static bool HasSchPromeGauge() => PromeGaugeType("SCH") != null;

    public static bool SchInDissipationOrGrace(double graceSeconds = 5)
    {
        var me = Me;
        if (me == null) return false;

        if (me.HasStatus(SchBuff.转化))
        {
            LastSchDissipationSeenAtUtc = DateTime.UtcNow;
            return true;
        }

        return LastSchDissipationSeenAtUtc is { } lastSeen
               && (DateTime.UtcNow - lastSeen).TotalSeconds <= graceSeconds;
    }

    public static bool TryAstActiveDraw(out string value)
    {
        if (TryReadPromeGaugeValue("AST", "ActiveDraw", out var raw) && raw != null)
        {
            value = raw.ToString() ?? "None";
            return true;
        }

        value = "None";
        return false;
    }

    public static string AstActiveDrawText()
        => TryAstActiveDraw(out var value) ? value : "None";

    public static string[] AstDrawnCards()
    {
        if ((!TryReadPromeGaugeValue("AST", "DrawnCards", out var raw)
             && !TryReadPromeGaugeValue("AST", "GetCardsTypes", out raw))
            || raw is not Array array)
            return Array.Empty<string>();

        return array.Cast<object>().Select(x => x?.ToString() ?? "None").ToArray();
    }

    public static string AstDrawnCard()
    {
        var cards = AstDrawnCards();
        return cards.Length > 0 ? cards[0] : "None";
    }

    public static bool HasAstDrawnCard()
    {
        return AstDrawnCards().Any(card => !string.IsNullOrWhiteSpace(card) && card != "None");
    }

    public static string AstDrawnDamageCard()
    {
        var card = AstDrawnCard();
        return IsAstDamageCard(card) ? card : "None";
    }

    public static bool HasAstDamageCard()
        => IsAstDamageCard(AstDrawnCard());

    public static string AstDrawnCrownCard()
        => TryReadPromeGaugeValue("AST", "DrawnCrownCard", out var raw)
           || TryReadPromeGaugeValue("AST", "GetCrownCardType", out raw)
            ? raw?.ToString() ?? "None"
            : "None";

    public static bool HasAstLordCard()
        => AstDrawnCrownCard() == "Lord";

    public static bool HasAstDivinationBuff()
        => Me?.HasStatus(AstBuff.占卜) == true;

    public static bool AstHasDivinationBuffWindow()
    {
        // 已经吃到占卜，或身上有神谕预备，都视为当前处于占卜爆发窗口。
        var me = Me;
        return me?.HasStatus(AstBuff.占卜) == true || me?.HasStatus(AstBuff.神谕预备) == true;
    }

    public static bool AstDivinationWindow()
    {
        // 实际 Buff 存在时优先按窗口内处理。
        if (AstHasDivinationBuffWindow())
            return true;

        // 没有 Buff 时，用占卜 CD 粗略描述 120s 窗口：
        // 冷却接近转好 <= 2.5s，或刚打出去后冷却 > 105s，都允许卡牌/王冠进入窗口。
        var cooldown = ActionCooldown(AstAction.占卜, 50);
        return cooldown > 105f || cooldown <= 2.5f;
    }

    public static bool AstCardWindow()
        // 开场前 10s 只认实际 Buff，避免普通循环抢起手；10s 后按 120s 占卜窗口判断。
        => BattleTimeAtLeast(10) ? AstDivinationWindow() : AstHasDivinationBuffWindow();

    public static byte SgeAddersgall()
        => TryReadPromeGauge<byte>("SGE", "Addersgall", out var value) ? value : (byte)0;

    public static byte SgeAddersting()
        => TryReadPromeGauge<byte>("SGE", "Addersting", out var value) ? value : (byte)0;

    public static bool SgeEukrasia()
        => TryReadPromeGauge<bool>("SGE", "Eukrasia", out var value) && value;

    public static short SgeAddersgallTimer()
        => TryReadPromeGauge<short>("SGE", "AddersgallTimer", out var value) ? value : (short)0;

    private static readonly Dictionary<string, Type?> PromeGaugeTypes = new();

    private static Type? PromeGaugeType(string gaugeName)
    {
        if (PromeGaugeTypes.TryGetValue(gaugeName, out var cached) && cached != null)
            return cached;

        var type =
            Type.GetType($"PromeRotation.Helpers.JobGaugeHelper+{gaugeName}, PromeRotation", false) ??
            Type.GetType($"PromeRotation.Helpers.JobGaugeHelper.{gaugeName}, PromeRotation", false);

        if (type == null)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != "PromeRotation") continue;

                type =
                    assembly.GetType($"PromeRotation.Helpers.JobGaugeHelper+{gaugeName}", false) ??
                    assembly.GetType($"PromeRotation.Helpers.JobGaugeHelper.{gaugeName}", false);

                if (type != null)
                    break;
            }
        }

        if (type != null)
            PromeGaugeTypes[gaugeName] = type;

        return type;
    }

    private static bool TryReadPromeGauge<T>(string gaugeName, string propertyName, out T value)
    {
        value = default!;

        try
        {
            var raw = PromeGaugeType(gaugeName)
                ?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null);

            if (raw is T typed)
            {
                value = typed;
                return true;
            }

            if (raw == null) return false;

            value = (T)Convert.ChangeType(raw, typeof(T));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadPromeGaugeValue(string gaugeName, string propertyName, out object? value)
    {
        value = null;

        try
        {
            value = PromeGaugeType(gaugeName)
                ?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null);
            return value != null;
        }
        catch
        {
            return false;
        }
    }
}
