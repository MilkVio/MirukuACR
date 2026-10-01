using System.Collections.Concurrent;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using MilkVio.DPS.Machinist.MCHData;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using GameActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using GameActionType = FFXIVClientStructs.FFXIV.Client.Game.ActionType;

namespace MilkVio.DPS.Machinist.Planning;

internal static class MachinistPlanning
{
    private readonly record struct Effect(ulong Source, ulong Target, uint Action, uint Sequence, long At);
    private static readonly ConcurrentQueue<Effect> Effects = new();
    private static readonly Queue<(uint Action, uint Sequence)> Seen = new();
    private static readonly HashSet<uint> WildfireSequences = new();
    private static readonly uint[] PartyBuffs = { 1822, 1848, 1825, 141, 2964, 2599, 1297, 2703, 3685, 1182, 1185, 786, 1878, 3887 };
    private static bool _entered;
    private static bool? _fullMetalQt;
    private static float _actionLock = .65f;
    private static long _budgetAt, _wildfireAt, _queenUntil, _hyperchargeAt, _lastWildfireAt;
    private static int _effectCount, _effectOverflow;
    private static ulong _wildfireTarget;
    private static int _wildfireHits;
    private static bool _wildfireLead, _leadGcdDone, _wildfireFast, _wildfireRecovery;
    private static readonly MachinistDispatch Dispatch = new();
    private static readonly MachinistPartyClock PartyClock = new();
    private static MachinistState _budgetState;
    private static MachinistBudget _budget;
    public static readonly MachinistOutputWindow Window = new();
    public static System.Action<string>? WriteNote;
    public static string Description { get; private set; } = "等待机工求解";

    public static void Enter()
    {
        ResetSession("进入MCH"); _entered = true;
        Dispatch.WriteNote = message => WriteNote?.Invoke(message);
        Window.Changed = (_, message) => { _budgetAt = 0; WriteNote?.Invoke(message); };
    }
    public static void Exit() { _entered = false; ResetSession("离开MCH"); Window.Changed = null; WriteNote = null; }
    // 隐藏QT只在一场战斗/职业结束时复位；死亡、复活、无目标和普通重算保留轴的控制。
    public static void ResetSession(string reason)
    {
        if (!PromeSettings.Instance.GetQt(MCHQt.全金属爆发))
            WriteNote?.Invoke($"[MCH] 全金属QT恢复开启：{reason}");
        PromeSettings.Instance.SetQt(MCHQt.全金属爆发, true);
        _fullMetalQt = null;
        Reset(reason, true);
    }
    public static void Reset(string reason, bool clearWindow = false)
    {
        Dispatch.Reset();
        PartyClock.Reset();
        _actionLock = .65f;
        _budgetAt = _wildfireAt = _queenUntil = _hyperchargeAt = _lastWildfireAt = 0;
        _wildfireTarget = 0; _wildfireHits = 0; _wildfireLead = _leadGcdDone = _wildfireFast = _wildfireRecovery = false;
        Seen.Clear(); WildfireSequences.Clear();
        while (Effects.TryDequeue(out _)) Interlocked.Decrement(ref _effectCount);
        if (clearWindow) Window.Reset(reason);
        Description = reason; WriteNote?.Invoke($"规划重置：{reason}");
    }

    public static void ObserveEffect(ulong source, ulong target, uint action, uint sequence, long now)
    {
        if (!_entered || Core.Me == null || source != Core.Me.EntityId) return;
        if (Interlocked.Increment(ref _effectCount) > 512)
        {
            Interlocked.Decrement(ref _effectCount); Interlocked.Exchange(ref _effectOverflow, 1); return;
        }
        Effects.Enqueue(new(source, target, action, sequence, now));
    }

    public static void Update()
    {
        if (!_entered) return;
        if (Interlocked.Exchange(ref _effectOverflow, 0) != 0) Reset("效果队列溢出，按实况恢复");
        var now = Environment.TickCount64;
        if (Core.Me == null || Core.Me.IsDead)
        {
            PartyClock.Reset();
            if (Dispatch.Pending || _wildfireAt != 0 || Dispatch.ReservedTool != 0) Reset("自身不可用");
            return;
        }
        // 先识别真实GCD，再把本帧效果归入该轮；候选和效果使用同一份本地预算。
        Dispatch.Update(ReadState(), GcdStarted(now), MachinistHost.Busy);
        while (Effects.TryDequeue(out var effect))
        {
            Interlocked.Decrement(ref _effectCount);
            if (effect.Source != Core.Me.EntityId) continue;
            // AOE按目标展开，野火只计自身在原目标上的一次武器技能效果。
            if (_wildfireAt > 0 && effect.At >= _wildfireAt && effect.At < _wildfireAt + 10000
                && effect.Target == _wildfireTarget && MachinistRules.IsWeaponskill(effect.Action)
                && WildfireSequences.Add(effect.Sequence))
            {
                _wildfireHits = Math.Min(6, _wildfireHits + 1);
                WriteNote?.Invoke($"野火目标={_wildfireTarget:X} 第{_wildfireHits}次武器技能效果={effect.Action} 距应用={(effect.At - _wildfireAt) / 1000f:F3}s seq={effect.Sequence}");
            }
            if (Seen.Contains((effect.Action, effect.Sequence))) continue;
            Seen.Enqueue((effect.Action, effect.Sequence)); if (Seen.Count > 64) Seen.Dequeue();
            _budgetAt = 0;
            if (ActionHelper.TryResolveActionType(effect.Action, out var type) && type == ActionType.OffGcd)
                Dispatch.ObserveAbility(effect.Action, effect.Sequence, effect.At);
            if (effect.Action == MCHSkill.超荷) _hyperchargeAt = effect.At;
            if (effect.Action == MCHSkill.后式自走人偶) _queenUntil = effect.At + (long)(MachinistQueen.Duration * 1000);
            if (effect.Action == MCHSkill.野火)
            {
                if (_lastWildfireAt > 0)
                    WriteNote?.Invoke($"野火间隔={(effect.At - _lastWildfireAt) / 1000f:F3}s " +
                        $"相对120秒漂移={(effect.At - _lastWildfireAt) / 1000f - 120:F3}s");
                _lastWildfireAt = effect.At;
                _wildfireAt = effect.At; _wildfireTarget = effect.Target; _wildfireHits = 0;
                WildfireSequences.Clear();
                _wildfireFast = Core.Me.Level == 100 && Dispatch.WasFastWildfire(effect.Sequence, effect.Target);
                _wildfireRecovery = Core.Me.Level == 100 && Dispatch.WasRecoveryWildfire(effect.Sequence, effect.Target);
                _wildfireLead = !_wildfireFast && !_wildfireRecovery && now - _hyperchargeAt > 2500 && !Core.Me.HasStatus(MCHStatus.过热);
                _leadGcdDone = false;
                WriteNote?.Invoke($"野火已应用 目标={_wildfireTarget:X} 路线={(_wildfireFast ? "快速野火，按资源直接衔接" : _wildfireRecovery ? "异常恢复，兑现已有过热" : _wildfireLead ? "普通GCD后超荷" : "超荷后野火")}");
            }
            if (MachinistRules.IsWeaponskill(effect.Action))
            {
                if (_wildfireLead && !MachinistRules.IsHeatShot(effect.Action)) _leadGcdDone = true;
            }
        }
        if (_wildfireAt > 0 && now >= _wildfireAt + 10000)
        {
            WriteNote?.Invoke($"野火观察结束：{_wildfireHits}/6次原目标武器技能效果");
            _wildfireAt = 0; _wildfireLead = _leadGcdDone = _wildfireFast = _wildfireRecovery = false;
        }
        var s = ReadState();
        if (_fullMetalQt != s.FullMetalQt)
        {
            _budgetAt = 0;
            if (_fullMetalQt != null || !s.FullMetalQt)
                WriteNote?.Invoke($"[MCH] 全金属QT={(s.FullMetalQt ? "开启，按当前优先级恢复" : "关闭，自动求解保留预备")}；预备剩余{s.FullMetal:F2}s");
            _fullMetalQt = s.FullMetalQt;
        }
        PartyClock.Observe(s);
        s.PartyCycle = PartyClock.StartIn(now);
        if ((_wildfireFast || _wildfireRecovery) && (!s.HasTarget || s.TargetId != _wildfireTarget || s.Level != 100))
        {
            WriteNote?.Invoke("野火特殊路线结束：目标不可用/切换目标/等级不再为100，按实况恢复");
            _wildfireAt = 0; _wildfireLead = _leadGcdDone = _wildfireFast = _wildfireRecovery = false; _budgetAt = 0;
            s = ReadState();
        }
        Dispatch.Update(s, GcdStarted(s.Now), MachinistHost.Busy);
        s.Weaves = Dispatch.Weaves;
        var elapsed = s.GcdTotal - s.GcdLeft;
        if (s.Lock > 0 && s.Weaves == 0 && elapsed is >= 0 and <= .2f)
        {
            _actionLock = Math.Clamp(s.Lock + elapsed, .3f, .8f);
            s = s with { ActionLock = _actionLock };
        }
        if (!s.Alive || !s.HasTarget) { _budgetAt = 0; return; }
        if (s.Level == 100 && (now - _budgetAt >= 150 || BudgetChanged(s, _budgetState)))
        {
            _budgetAt = now; _budgetState = s; _budget = MachinistProjection.Evaluate(s);
            Description = _budget.Reason;
        }
    }

    private static bool BudgetChanged(MachinistState a, MachinistState b) => a.Heat != b.Heat || a.Battery != b.Battery
        || a.QtKey != b.QtKey || a.WindowVersion != b.WindowVersion || a.TargetId != b.TargetId
        || a.Targets != b.Targets || a.Weaves != b.Weaves || a.WeaveLimit != b.WeaveLimit || a.Level != b.Level
        || a.FastWildfireActive != b.FastWildfireActive || a.WildfireRecovery != b.WildfireRecovery
        || Math.Abs(a.ActionLock - b.ActionLock) > .05f
        || (a.WindowLeft > 0) != (b.WindowLeft > 0) || (a.ReserveLeft > 0) != (b.ReserveLeft > 0)
        || (a.QueenLeft > 0) != (b.QueenLeft > 0) || (a.WildfireCd > 0) != (b.WildfireCd > 0)
        || (a.BarrelCd > 0) != (b.BarrelCd > 0) || (a.Reassemble > 0) != (b.Reassemble > 0)
        || a.OverheatStacks != b.OverheatStacks || (a.FreeHypercharge > 0) != (b.FreeHypercharge > 0)
        || (a.DamageWindow > 0) != (b.DamageWindow > 0);

    public static PAction? NextGcd(out string reason)
    {
        Update();
        var s = ReadState(); reason = "自身/目标不可用";
        if (!s.Alive || !s.HasTarget) return null;
        var choice = Dispatch.NextGcd(s);
        reason = choice.Reason;
        if (!s.FullMetalQt && s.FullMetal > 0)
            reason += "；全金属QT关闭，保留预备";
        // 交还宿主前复查开关；已经交出的宿主队列不在本地撤回。
        if (choice.Action == MCHSkill.全金属爆发 && !PromeSettings.Instance.GetQt(MCHQt.全金属爆发))
        { reason = "全金属QT已关闭，重新求解"; return null; }
        return choice.Action == 0 ? null : new(choice.Action, ActionType.Gcd, ActionTargetType.Target);
    }

    public static PAction? NextOffGcd(Func<PAction?>? legacyNextGcd, out string reason)
    {
        Update();
        var s = ReadState();
        var tool = s.Level == 100 ? MachinistRules.Gcd(s).Action : legacyNextGcd?.Invoke()?.ActionId ?? 0;
        var choice = Dispatch.NextOffGcd(s, _budget, MachinistHost.Busy, tool);
        reason = choice.Reason;
        if (choice.Action == 0) return null;
        var action = new PAction(choice.Action, ActionType.OffGcd,
            choice.Action is MCHSkill.野火 or MCHSkill.双将 or MCHSkill.将死 ? ActionTargetType.Target : ActionTargetType.Self);
        if (!NativeReady(action, out var status)) { reason = $"能力技尚不可提交：{choice.Action} 状态={status}"; return null; }
        return Dispatch.Issue(s, action.ActionId, action.ActionId.GetAdjustedActionId(),
            choice.Action == MCHSkill.整备 ? choice.Tool != 0 ? choice.Tool : tool : 0) ? action : null;
    }

    public static uint HeldTool => Dispatch.HeldTool(ReadState());

    public static PAction? PrepareLegacyOffGcd(PAction action)
    {
        var s = ReadState();
        if (Dispatch.Pending || MachinistHost.Busy || !s.Alive || !s.HasTarget || s.Lock > 0
            || MachinistRules.Slots(s) == 0 || s.GcdLeft <= Math.Max(.5f, s.WeaveLock) + .08f
            || !NativeReady(action, out _)) return null;
        return Dispatch.Issue(s, action.ActionId, action.ActionId.GetAdjustedActionId()) ? action : null;
    }

    private static unsafe bool NativeReady(PAction action, out uint status)
    {
        status = uint.MaxValue;
        var manager = GameActionManager.Instance();
        var target = action.Target == ActionTargetType.Self ? Core.Me : Core.Target;
        if (manager == null || target == null || target.IsDead || !target.IsTargetable) return false;
        status = manager->GetActionStatus(GameActionType.Action, action.ActionId, target.GameObjectId);
        return status == 0;
    }

    private static unsafe long GcdStarted(long now)
    {
        var manager = GameActionManager.Instance();
        var recast = manager == null ? null : manager->GetRecastGroupDetail(57);
        return recast is null ? Dispatch.RecastStarted(now, false, 0, 0, 0)
            : Dispatch.RecastStarted(now, recast->IsActive, recast->ActionId, recast->Total, recast->Elapsed);
    }

    public static unsafe MachinistState ReadState()
    {
        var now = Environment.TickCount64;
        var me = Core.Me;
        if (me == null) return new() { Now = now };
        var manager = GameActionManager.Instance();
        if (manager == null) return new() { Now = now };
        var recast = manager->GetRecastGroupDetail(57);
        static float Cd(uint id) => Math.Max(0, id.GetActionCooldown());
        static float Recast(uint id, float fallback)
        {
            var value = GameActionManager.GetAdjustedRecastTime(GameActionType.Action, id) / 1000f;
            return float.IsFinite(value) && value > .1f ? value : fallback;
        }
        bool Qt(string key) => PromeSettings.Instance.GetQt(key);
        var target = Core.Target;
        var gcd = Recast(MCHSkill.热分裂弹1, 2.5f);
        var s = new MachinistState
        {
            Now = now, PlayerId = me.EntityId, TargetId = target?.EntityId ?? 0, Level = me.Level,
            Alive = !me.IsDead && me.ClassJob.RowId == (uint)Job.MCH, InCombat = GameData.IsInCombat(),
            HasTarget = target != null && target.EntityId != me.EntityId && target.IsTargetable && !target.IsDead
                && me.DistanceToMe() <= GameData.GetCurrentAttackRange(25),
            Targets = Qt(MCHQt.AOE) && target != null && me.DistanceToMe() <= 12
                ? TargetHelper.GetEnemyCountInsideSector(me, target, 12, 90) : 1,
            Heat = JobGaugeHelper.MCH.Heat, Battery = JobGaugeHelper.MCH.Battery,
            Gcd = gcd, HyperchargeRecoveryLeft = Dispatch.HyperchargeRecoveryLeft(now, gcd),
            GcdLeft = recast != null && recast->IsActive
                ? Math.Min(Math.Max(0, recast->Total - recast->Elapsed), Math.Max(0, ActionHelper.GetGcdRemain())) : 0,
            GcdTotal = recast != null && recast->IsActive ? recast->Total : 0, Lock = Math.Max(0, ActionHelper.GetAnimationLock()),
            ActionLock = _actionLock,
            WeaveLock = .65f, Weaves = Dispatch.Weaves, MaxWeaves = MachinistHost.MaxWeaves, NormalMaxWeaves = 2,
            Combo = ActionHelper.GetLastComboID(), ComboLeft = ActionHelper.GetComboLeftTime(),
            Drill = MachinistHelper.GetCurrent钻头Charge(), DrillRecast = me.Level >= 94 ? 20 : Recast(MCHSkill.钻头, 20),
            AnchorCd = Cd(MachinistHelper.Get空气锚CurrentId()), AnchorRecast = Recast(MachinistHelper.Get空气锚CurrentId(), 40),
            SawCd = me.Level >= 90 ? Cd(MCHSkill.回转飞锯) : 999, SawRecast = Recast(MCHSkill.回转飞锯, 60),
            WildfireCd = Cd(MCHSkill.野火), BarrelCd = Cd(MCHSkill.枪管加热), HyperchargeCd = Cd(MCHSkill.超荷),
            ReassembleCharges = MachinistHelper.GetCurrent整备Charge(), Reassemble = me.GetStatusLeftTime(MCHStatus.整备),
            Gauss = Math.Clamp(3 - Cd(MachinistHelper.Get虹吸弹CurrentId()) / 30, 0, 3),
            Ricochet = Math.Clamp(3 - Cd(MachinistHelper.Get弹射CurrentId()) / 30, 0, 3),
            FreeHypercharge = me.GetStatusLeftTime(MCHStatus.超荷预备), Excavator = me.GetStatusLeftTime(MCHStatus.掘地飞轮预备),
            FullMetal = me.GetStatusLeftTime(MCHStatus.全金属爆发预备), Overheat = me.GetStatusLeftTime(MCHStatus.过热),
            OverheatStacks = me.HasStatus(MCHStatus.过热) ? Math.Clamp(me.GetStatusStackCount(MCHStatus.过热), 1, 5) : 0,
            QueenLeft = JobGaugeHelper.MCH.IsRobotActive ? Math.Max(1, (_queenUntil - now) / 1000f) : 0,
            QueenCd = Cd(MachinistHelper.GetCurrentRobotActionId()),
            WildfireLeft = _wildfireAt > 0 && target?.EntityId == _wildfireTarget ? Math.Max(0, 10 - (now - _wildfireAt) / 1000f) : 0,
            WildfireHits = _wildfireHits, WildfireLead = _wildfireLead, LeadGcdDone = _leadGcdDone,
            FastWildfire = _wildfireFast, WildfireRecovery = _wildfireRecovery,
            PartyLeft = PartyBuffs.Max(id => me.GetStatusLeftTime(id)), PartyCycle = PartyClock.StartIn(now), PotionLeft = me.GetStatusLeftTime(49),
            WildfireQt = Qt(MCHQt.野火), HyperchargeQt = Qt(MCHQt.超荷), BarrelQt = Qt(MCHQt.枪管加热), FullMetalQt = Qt(MCHQt.全金属爆发),
            QueenQt = Qt(MCHQt.机器人), ReassembleQt = Qt(MCHQt.整备), AoeQt = Qt(MCHQt.AOE),
            SawFirst = Qt(MCHQt.先打飞锯), DumpQt = Qt(MCHQt.倾泻资源), FastBurstQt = Qt(MCHQt.快速爆发)
        };
        var detection = Window.DetectionNeeded(now);
        return Window.Update(s, detection.Enemies ? TargetHelper.IsAllBossUntargetable() : null,
            detection.Weather ? GameData.Weather : null);
    }
}
