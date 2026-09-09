using System.Collections.Concurrent;
using MilkVio.DPS.Reaper.ReaperData;
using PromeRotation.Data;

namespace MilkVio.DPS.Reaper;

public enum ReaperBurstPhase { 非爆发期, 准备, 第一附体, 大丰收衔接, 第二附体, 收尾, 起手 }

public sealed class ReaperBurstPlanner
{
    private readonly ConcurrentQueue<(ulong Source, uint Id, uint Sequence, long At)> _events = new();
    private readonly Queue<(uint Sequence, uint Id)> _seen = new();
    private readonly Queue<string> _recent = new();
    private long _phaseAt, _progressAt, _retryAt, _simpleUntil, _resetAt;
    private long _coordinateUntil, _coordinateCooldown;
    private long _sliceQueueUntil, _harvestQueueUntil;
    private bool _harvestWaitUsed;
    private long _shroudSyncUntil;
    private bool _qtKnown, _circleQt, _enshroudQt;
    private bool _firstDot, _skipFirstDot, _harvestUsed, _finishComboDone, _perfectioDone;
    private int _prepGcds, _openerStep, _executions;
    private (int Step, uint Id, uint? Status)? _openerBlocked;
    private uint _lastRecordedGcd;
    private long _lastRecordedGcdAt;
    private ReaperState _previous;
    private bool _forecastOnly;
    internal ReaperBurstRoute RecoveryRoute { get; private set; }
    private ReaperState _recoveryState;
    private long _recoveryUntil;
    private readonly ReaperProjection _projection = new();
    private readonly ReaperDumpPlanner _dump = new();
    public string ProjectionDebug => IsOpener ? "固定起手" : Current.IsDump ? _dump.Debug : _projection.Debug;
    public string ModeName => IsOpener ? "起手" : Current.IsDump ? "倾泻" : IsSimple ? "兜底"
        : !IsPlanned && Current.CircleLeft > 0 ? "非标准爆发" : Phase.ToString();

    internal ReaperBurstPlanner ForkForForecast() => new()
    {
        _forecastOnly = true, Current = Current, _previous = _previous, Phase = Phase,
        _phaseAt = _phaseAt, _progressAt = _progressAt, _retryAt = _retryAt, _simpleUntil = _simpleUntil,
        _coordinateUntil = _coordinateUntil, _coordinateCooldown = _coordinateCooldown,
        _qtKnown = _qtKnown, _circleQt = _circleQt, _enshroudQt = _enshroudQt,
        _firstDot = _firstDot, _skipFirstDot = _skipFirstDot,
        _harvestUsed = _harvestUsed, _finishComboDone = _finishComboDone, _perfectioDone = _perfectioDone,
        _prepGcds = _prepGcds, _openerStep = _openerStep, _executions = _executions,
        _lastRecordedGcd = _lastRecordedGcd, _lastRecordedGcdAt = _lastRecordedGcdAt,
        _shroudSyncUntil = _shroudSyncUntil,
        RecoveryRoute = RecoveryRoute, _recoveryState = _recoveryState, _recoveryUntil = _recoveryUntil
    };

    public ReaperState Current { get; private set; }
    public ReaperBurstPhase Phase { get; private set; }
    public string Reason { get; private set; } = "非爆发期";
    public uint GcdAction { get; private set; }
    public uint OffGcdAction { get; private set; }
    public int ForecastGreen { get; private set; } = -1;
    public string ForecastGreenText => ForecastGreen >= 0 ? ForecastGreen.ToString()
        : !Current.HasTiming ? "未读取复唱" : Current.IsDump ? "倾泻不预留"
        : !Current.CircleQt ? "120关闭" : Current.CircleLeft > 0 ? "当前团辅中"
        : Current.Locked ? "当前技能链未结束" : Current.FreeEnshroud > 0 ? "本次免费"
        : Current.Shroud < 50 ? "当前绿不足50" : "当前不作普通附体预测";
    public float? BurstFinishIn { get; private set; }
    public float? BurstCommunioIn { get; private set; }
    public string FallbackReason { get; private set; } = "";
    public bool IsPlanned => Phase != ReaperBurstPhase.非爆发期;
    public bool IsOpener => Phase == ReaperBurstPhase.起手;
    public bool IsCoordinating => _coordinateUntil > Current.Now;
    public bool IsSimple => Current.Now < _simpleUntil;
    public bool RulesDisabled { get; private set; }
    public bool DeferSliceQueue { get; private set; }
    public bool DeferHarvestQueue { get; private set; }
    public IEnumerable<string> RecentActions => _recent;

    public void EnqueueAction(ulong source, uint id, uint sequence)
    {
        if (source != Current.PlayerId || _events.Count >= 64) return;
        _events.Enqueue((source, id, sequence, Environment.TickCount64));
    }

    public void Reset(string reason = "重置")
    {
        RecoveryRoute = ReaperBurstRoute.当前安排; _recoveryUntil = 0;
        _events.Clear(); _seen.Clear(); _recent.Clear();
        _resetAt = Environment.TickCount64;
        _phaseAt = _progressAt = _retryAt = _simpleUntil = _coordinateUntil = _coordinateCooldown = 0;
        _sliceQueueUntil = 0; DeferSliceQueue = false;
        _harvestQueueUntil = 0; DeferHarvestQueue = false;
        _harvestWaitUsed = false;
        _shroudSyncUntil = 0;
        _qtKnown = _firstDot = _skipFirstDot = _harvestUsed = _finishComboDone = _perfectioDone = false;
        _prepGcds = _openerStep = _executions = 0;
        _openerBlocked = null;
        _lastRecordedGcd = 0; _lastRecordedGcdAt = 0;
        Phase = ReaperBurstPhase.非爆发期; Reason = reason;
        GcdAction = OffGcdAction = 0; ForecastGreen = -1;
        BurstFinishIn = BurstCommunioIn = null; FallbackReason = "";
        Current = _previous = default;
        RulesDisabled = false;
        _projection.Clear();
        _dump.Clear();
    }

    public void Cancel(string reason)
    {
        StopHarvestWait(reason);
        _skipFirstDot = false;
        RecoveryRoute = ReaperBurstRoute.当前安排; _recoveryUntil = 0;
        Phase = ReaperBurstPhase.非爆发期;
        GcdAction = OffGcdAction = 0;
        _sliceQueueUntil = 0; DeferSliceQueue = false;
        _projection.Clear();
        _retryAt = Current.Now + 15000;
        _simpleUntil = Current.Now + (long)(Math.Max(5, Current.CircleLeft) * 1000);
        Reason = reason;
        FallbackReason = reason;
    }

    public void Replan(string reason)
    {
        StopHarvestWait(reason);
        RecoveryRoute = ReaperBurstRoute.当前安排; _recoveryUntil = 0;
        Phase = ReaperBurstPhase.非爆发期;
        GcdAction = OffGcdAction = 0;
        _retryAt = _simpleUntil = _sliceQueueUntil = 0;
        _prepGcds = 0;
        _firstDot = _skipFirstDot = _harvestUsed = _finishComboDone = _perfectioDone = false;
        DeferSliceQueue = false;
        Reason = reason;
        _projection.Clear();
        _dump.Clear();
    }

    public void WindowChanged(string reason)
    {
        // 正在执行且仍能完成的爆发由Update检查，不因窗口来源变化直接丢进度。
        _projection.Clear();
        if (!IsPlanned && RecoveryRoute == ReaperBurstRoute.当前安排) Replan(reason);
    }

    private void ValidateRecovery(ReaperState s)
    {
        if (RecoveryRoute == ReaperBurstRoute.当前安排) return;
        var old = _recoveryState;
        var windowChanged = s.WindowVersion != old.WindowVersion || s.WindowActive != old.WindowActive;
        var oldEnd = old.Now / 1000d + Math.Min(old.CircleLeft, old.WindowActive ? old.WindowLeft : float.PositiveInfinity);
        var newEnd = s.Now / 1000d + Math.Min(s.CircleLeft, s.WindowActive ? s.WindowLeft : float.PositiveInfinity);
        var changedCoverage = windowChanged && (Math.Abs(newEnd - oldEnd) > .25
            || s.GoalSoul > 0 || s.GoalShroud > 0 || old.GoalSoul > 0 || old.GoalShroud > 0);
        if (s.Now >= _recoveryUntil || !s.HasTarget || s.TargetId != old.TargetId
            || s.CircleLeft <= 0 || s.SacrificeStacks == 0 || changedCoverage
            || s.EnshroudQt != old.EnshroudQt || s.CircleQt != old.CircleQt || s.BloodQt != old.BloodQt || s.GluttonyQt != old.GluttonyQt
            || s.SliceQt != old.SliceQt || s.DotQt != old.DotQt || s.HarvestMoonQt != old.HarvestMoonQt
            || s.GoalSoul != old.GoalSoul || s.GoalShroud != old.GoalShroud || !ReaperBurstRecovery.PendingHarvest(s)
            || Math.Abs(s.Gcd - old.Gcd) > .001f || Math.Abs(s.CommunioCast - old.CommunioCast) > .001f
            || Math.Abs(s.ReapGcd - old.ReapGcd) > .001f || Math.Abs(s.PerfectioGcd - old.PerfectioGcd) > .001f)
        { RecoveryRoute = ReaperBurstRoute.当前安排; _projection.Clear(); }
        else if (windowChanged) _recoveryState = s;
    }

    public void DisableRules()
    {
        Cancel("读取或规划异常，转基础循环");
        RulesDisabled = true;
    }

    public void NoteDumpFallback(uint id, bool off)
    {
        if (off) OffGcdAction = id;
        else GcdAction = id;
        Reason = "倾泻：推荐技能不可用，改用当前可执行技能";
        FallbackReason = Reason;
    }

    public void BeginOpener(ReaperState s)
    {
        Current = s;
        if (!s.Alive || !s.HasTarget || s.Level < 100 || !s.HasTiming
            || s.Locked || s.CircleCd > 0 || s.GluttonyCd > 0 || s.SliceCharges < 1) return;
        _coordinateUntil = _coordinateCooldown = _simpleUntil = 0;
        _projection.Clear(); _dump.Clear();
        _openerStep = _executions = 0;
        _openerBlocked = null;
        _firstDot = _skipFirstDot = _harvestUsed = _perfectioDone = _finishComboDone = false;
        SetPhase(ReaperBurstPhase.起手);
        PromeSettings.Instance.OpenerHasBeenExecuted = true;
    }

    // 只用于固定步骤内部的可用性计算，不修改实况或真实QT。
    private static ReaperState OpenerState(ReaperState s) => s with
        { EnshroudQt = true, HarvestMoonQt = false, DumpQt = false, WindowActive = false };

    internal void NoteOpenerBlocked(uint id, uint? nativeStatus)
    {
        var blocked = (_openerStep, id, nativeStatus);
        if (!IsOpener || _openerBlocked == blocked) return;
        _openerBlocked = blocked;
        if (!_forecastOnly) ReaperBattleData.Instance.DebugLog.Note(Current.Now,
            $"固定起手第{_openerStep + 1}步暂不可用：技能={id} 原生状态={nativeStatus?.ToString() ?? "冷却/目标/距离等条件未满足"}");
    }

    private bool UpdateOpener(ReaperState s)
    {
        if (!IsOpener) return false;
        if (!ReaperSettings.Instance.启用起手 || PromeSettings.Instance.EnableAcr is AcrState.Off or AcrState.Hold
            || !s.HasTarget || !s.HasTiming || _previous.PlayerId != 0 && s.PlayerId != _previous.PlayerId
            || _previous.TargetId != 0 && s.TargetId != _previous.TargetId)
        { Replan("起手条件失效，交回普通循环"); return false; }
        Reconcile(s);
        if (s.Now - _phaseAt > 40000 || s.Now - _progressAt > Math.Max(9000, 3 * s.Gcd * 1000 + 2000))
        { Cancel("起手未推进，交回普通循环"); return false; }
        Reason = $"固定起手第{_openerStep + 1}步";
        BuildOpener(OpenerState(s));
        if (!IsOpener) return false;
        (BurstCommunioIn, BurstFinishIn) = EstimateBurstFinish(s);
        UpdateHarvestQueue(s);
        _previous = s;
        return true;
    }

    private void SetPhase(ReaperBurstPhase phase)
    {
        Phase = phase; _phaseAt = _progressAt = Current.Now;
        Reason = phase.ToString();
    }

    // 只在宿主每帧更新/真正选招时调用；调试Check只读结果。
    public void Update(ReaperState s, bool evaluateResources = false)
    {
        if (s.WindowActive) s = s with { DumpQt = false };
        RulesDisabled = false;
        Current = s;
        GcdAction = OffGcdAction = 0; ForecastGreen = -1;
        BurstFinishIn = BurstCommunioIn = null;
        DeferSliceQueue = false;
        DeferHarvestQueue = false;
        if (s.FastCircle != _previous.FastCircle)
        {
            // 只重算时机，不丢弃已经完成的烙印/附体进度。
            _projection.Clear(); _dump.Clear();
            RecoveryRoute = ReaperBurstRoute.当前安排; _recoveryUntil = 0;
        }
        if (!s.Alive || !s.InCombat)
        {
            if (_previous.InCombat || IsPlanned) Reset(s.Alive ? "脱战" : "死亡/未加载");
            Current = s; ObserveQts(s); _previous = s;
            return;
        }
        // 复唱先刷新、量谱稍后扣层时，不用这半份状态取消爆发；出招仍读取真实量谱。
        if (s.Enshrouded <= 0 || s.CastingCommunio || s.Lemure != _previous.Lemure || s.TargetId != _previous.TargetId)
            _shroudSyncUntil = 0;
        else if (_previous.Enshrouded > 0 && s.GcdElapsed + 0.1f < _previous.GcdElapsed)
            _shroudSyncUntil = s.Now + 500;
        while (_events.TryDequeue(out var effect))
        {
            if (effect.Source != s.PlayerId || effect.At < _resetAt || _seen.Contains((effect.Sequence, effect.Id))) continue;
            _seen.Enqueue((effect.Sequence, effect.Id));
            if (_seen.Count > 32) _seen.Dequeue();
            ObserveAction(effect.Id);
        }
        // 复唱组只用来校正已经开始的GCD；团契必须等附体结束，不能把开读条当成成功。
        if (s.LastGcd != 0 && (s.LastGcd != _previous.LastGcd || s.GcdElapsed + 0.1f < _previous.GcdElapsed)
            && !(s.LastGcd == ReaperSkill.死亡之影 && s.Enshrouded > 0 && Math.Abs(s.GcdLeft + s.GcdElapsed - s.Gcd) > 0.1f)
            && (s.LastGcd != ReaperSkill.团契 || s.Enshrouded <= 0)) ObserveAction(s.LastGcd);
        if (s.LastGcd == ReaperSkill.团契 && _previous.Enshrouded > 0 && s.Enshrouded <= 0)
            ObserveAction(s.LastGcd);
        // 固定序列独占出招；QT、倾泻和窗口只保留最新实况，结束后再参与普通决策。
        if (UpdateOpener(s)) return;
        if (s.IsDump != _previous.IsDump)
        {
            Replan("倾泻模式变化，按当前资源重算");
            _coordinateUntil = _coordinateCooldown = 0;
            _qtKnown = false;
            FallbackReason = "";
        }
        if (s.IsDump)
        {
            (GcdAction, OffGcdAction, Reason) = ReaperDumpPlanner.Build(s);
            if (!_forecastOnly)
            {
                try
                {
                    var advice = _dump.Choose(this, s, GcdAction, OffGcdAction, evaluateResources);
                    if (advice.HasValue)
                    {
                        if (advice.Value.OffGcd) OffGcdAction = advice.Value.Action;
                        else GcdAction = advice.Value.Action;
                        Reason = advice.Value.Reason;
                    }
                }
                catch (Exception) { Reason = "倾泻：推演异常，使用倾泻基础规则"; _dump.Clear(); }
            }
            UpdateHarvestQueue(s);
            _previous = s;
            return;
        }
        ObserveQts(s);
        if (_previous.Alive && (s.WindowActive != _previous.WindowActive || s.WindowVersion != _previous.WindowVersion))
            WindowChanged("输出窗口变化，按实况重算");
        if (_previous.Alive && ((!_previous.EnshroudQt && s.EnshroudQt) || (!_previous.CircleQt && s.CircleQt)))
        {
            _retryAt = _simpleUntil = 0;
        }
        if (IsPlanned && (!s.HasTarget || (_previous.TargetId != 0 && s.TargetId != _previous.TargetId)))
            Replan("目标变化，按实况继续");
        if (IsPlanned && (!s.HasTiming || (!s.EnshroudQt && s.Enshrouded <= 0)
            || (!s.DotQt && Phase == ReaperBurstPhase.准备)
            || (!s.CircleQt && s.CircleLeft <= 0 && s.CircleCd < 60)))
            Replan("时序或QT变化，按当前权限重算");

        Reconcile(s);
        if (IsPlanned && _previous.CircleLeft > 0 && s.CircleLeft <= 0
            && Phase != ReaperBurstPhase.收尾 && !(Phase == ReaperBurstPhase.起手 && _openerStep >= 8))
            Cancel("神秘环已结束，按当前资源收尾");
        if (IsPlanned)
        {
            var limit = Phase == ReaperBurstPhase.起手 ? 40000 :
                (long)((Phase == ReaperBurstPhase.准备 ? s.PreparationLead + 3 * s.Gcd : s.SingleDuration + 3 * s.Gcd + 3) * 1000);
            if (s.Now - _phaseAt > limit || s.Now - _progressAt > Math.Max(9000, 3 * s.Gcd * 1000 + 2000))
                Cancel("计划未推进，转简单循环");
        }
        if (!s.HasTarget) { StopHarvestWait("目标不可用"); _previous = s; return; }
        if (s.HasTiming && s.Level >= 80 && s.CircleQt && s.CircleLeft <= 0 && s.Shroud >= 50
            && !s.Locked && s.FreeEnshroud <= 0)
            ForecastGreen = ReaperResources.ForecastShroud(s, true);
        if (Phase == ReaperBurstPhase.非爆发期 && s.Now >= _retryAt && !IsCoordinating && !IsSimple
            && s.Level >= 100 && s.HasTiming && s.Melee && s.CircleQt && s.EnshroudQt && s.DotQt
            && s.CircleLeft <= 0 && s.CircleCd > 0 && s.CircleCd <= s.PreparationLead
            && s.Shroud >= 50 && s.FreeEnshroud <= 0 && s.Perfectio <= 0 && s.Occulta <= 0 && !s.Locked
            && (!s.WindowActive || s.WindowLeft > s.CircleCd + 20 || ReaperResources.ForecastPreparation(s, false).Ready))
        {
            _prepGcds = 0; _firstDot = _skipFirstDot = _harvestUsed = _perfectioDone = _finishComboDone = false;
            SetPhase(ReaperBurstPhase.准备);
        }

        if (IsPlanned)
        {
            Reason = Phase.ToString();
            BuildPlan(s);
            (BurstCommunioIn, BurstFinishIn) = EstimateBurstFinish(s);
            if (BurstCommunioIn.HasValue && s.CircleLeft > 0 && BurstCommunioIn.Value + 0.15f >= s.CircleLeft)
                Cancel("剩余神秘环不足，保住当前附体后按实况收尾");
            var windowFinish = BurstCommunioIn ?? BurstFinishIn;
            if (IsPlanned && s.WindowActive && windowFinish.HasValue && windowFinish.Value + 0.15f >= s.WindowLeft)
                Replan("剩余输出窗口不足，按当前资源收尾");
        }
        if (!IsPlanned) BuildNormal(s);
        ValidateRecovery(s);
        if (!IsPlanned && ReaperBurstRecovery.PrioritizeHarvestReward(s))
        {
            RecoveryRoute = ReaperBurstRoute.当前安排;
            _recoveryUntil = 0;
        }
        else if (!IsPlanned && RecoveryRoute != ReaperBurstRoute.当前安排)
        {
            (GcdAction, OffGcdAction) = ReaperBurstRecovery.Apply(s, RecoveryRoute, GcdAction, OffGcdAction);
            Reason = $"本轮衔接：{RecoveryRoute}";
        }
        ApplyWindowLimits(s);
        if (!_forecastOnly && !IsCoordinating && s.HasTiming && s.Alive && s.HasTarget && s.Level >= 100
            && (ReaperResources.IsWindowClosing(s) || s.WindowActive && (s.GoalSoul > 0 || s.GoalShroud > 0)
                || !s.DumpQt && !IsPlanned && (ReaperBurstRecovery.IsActive(s)
                    || s.Shroud >= 70 || s.Soul >= 90 && s.SliceCharges >= 1.8f)))
        {
            var advice = _projection.Choose(this, s, GcdAction, OffGcdAction, evaluateResources);
            if (advice.HasValue)
            {
                if (advice.Value.OffGcd) OffGcdAction = advice.Value.Action;
                else GcdAction = advice.Value.Action;
                Reason = advice.Value.Reason;
                BurstFinishIn ??= _projection.BuffFinishIn;
                BurstCommunioIn ??= _projection.BuffCommunioIn;
                if (RecoveryRoute == ReaperBurstRoute.当前安排 && advice.Value.Route != ReaperBurstRoute.当前安排
                    && s.CircleLeft > 0 && ReaperBurstRecovery.PendingHarvest(s))
                {
                    RecoveryRoute = advice.Value.Route; _recoveryState = s;
                    _recoveryUntil = s.Now + (long)(Math.Min(30, s.CircleLeft) * 1000);
                }
            }
        }
        UpdateHarvestQueue(s);
        UpdateSliceQueue(s);
        if (GcdAction == ReaperSkill.收获月 && s.Enshrouded > 0)
            Reason = s.CanHarvestMoonForRange ? "附体远离/移动使用收获月" : "团契来不及读完，使用收获月";
        else if (s.Enshrouded > 0 && s.Lemure == 1 && s.Moving && GcdAction == 0)
            Reason = "等待可读条时机，保留团契";
        _previous = s;
    }

    private void ApplyWindowLimits(ReaperState s)
    {
        if (!s.WindowActive) return;
        if (OffGcdAction == ReaperSkill.夜游魂衣 && s.FreeEnshroud <= 0 && s.Shroud - 50 < s.GoalShroud)
            OffGcdAction = 0;
        if (OffGcdAction is ReaperSkill.暴食 or ReaperSkill.隐匿挥割 && s.Soul - 50 < s.GoalSoul)
            OffGcdAction = 0;
        if (s.WindowLeft <= 0 && s.WindowLimit > 0) Reason = "等待结束信号，保护期末资源";
    }

    private void UpdateSliceQueue(ReaperState s)
    {
        var chargeIn = (1 - s.SliceCharges) * s.SliceRecast;
        if (IsPlanned || IsSimple || IsCoordinating || s.DumpQt || s.CircleLeft > 0
            || s.CircleQt && s.CircleCd <= s.PreparationLead
            || GcdAction != 0 || OffGcdAction != 0 || !s.HasTiming || !s.Melee || s.Locked
            || !s.SliceQt || s.Level < 60 || s.Soul > 50 || s.SliceCharges >= 1
            || !float.IsFinite(s.GcdLeft) || s.GcdLeft <= 0 || s.GcdLeft > 0.3f || !float.IsFinite(chargeIn) || chargeIn <= 0
            || chargeIn > s.GcdLeft || s.ComboAtRisk(s.Gcd))
        {
            _sliceQueueUntil = 0;
            return;
        }
        // 只推迟宿主的提前预排；公共冷却一结束，立即放行其他技能。
        if (_sliceQueueUntil == 0) _sliceQueueUntil = s.Now + (long)(s.GcdLeft * 1000);
        DeferSliceQueue = s.Now < _sliceQueueUntil;
        if (DeferSliceQueue) Reason = "灵魂切割将在本次复唱结束前恢复";
    }

    private void UpdateHarvestQueue(ReaperState s)
    {
        // 漏掉神秘环效果事件时，用实际冷却进入新一轮校正；不以Buff在0附近抖动重新发放额度。
        if (_previous.InCombat && _previous.CircleCd <= 1 && s.CircleCd > 60 && s.Bloodsown > 3)
        { StopHarvestWait("新一轮神秘环"); _harvestWaitUsed = false; }
        if (s.TargetId != _previous.TargetId || !IsOpener && (s.EnshroudQt != _previous.EnshroudQt || s.CircleQt != _previous.CircleQt
            || s.DotQt != _previous.DotQt || s.GluttonyQt != _previous.GluttonyQt || s.BloodQt != _previous.BloodQt
            || s.SliceQt != _previous.SliceQt || s.HarvestMoonQt != _previous.HarvestMoonQt
            || s.WindowVersion != _previous.WindowVersion || s.WindowActive != _previous.WindowActive))
            StopHarvestWait("目标/QT/窗口变化");
        if (!CanWaitForHarvest) StopHarvestWait("等待条件失效");
        else if (_harvestQueueUntil > 0 && s.Now >= _harvestQueueUntil) StopHarvestWait("等待超时，恢复普通求解");

        // 被动更新和预测只给出等待意图，实际选招才启动看门狗。
        DeferHarvestQueue = CanWaitForHarvest && (_harvestQueueUntil > 0 || !_harvestWaitUsed && s.Bloodsown > 0);
        if (DeferHarvestQueue)
        {
            Reason = "短等大丰收解锁";
            if (HarvestConflicts(OffGcdAction)) OffGcdAction = 0;
        }
    }

    internal bool CanWaitForHarvest => !RulesDisabled && !IsCoordinating && GcdAction == ReaperSkill.大丰收
        && Current.Alive && Current.InCombat && Current.HasTarget
        && ReaperBurstRecovery.HarvestNextGcd(IsOpener ? OpenerState(Current) : Current)
        && (IsOpener || !(Current.DotQt && Current.Melee && Current.DeathDesign <= ReaperBurstRecovery.HarvestAt(Current) + 0.15f)
            && (!Current.WindowActive || Current.WindowLeft > ReaperBurstRecovery.HarvestAt(Current) + 0.65f));

    // 每次机会只启动一次。真实经过时间独立于快照/Buff计时，提交动作也不续期。
    internal bool HoldHarvest(long now, string unavailable = "")
    {
        if (!CanWaitForHarvest) { StopHarvestWait("等待条件失效"); return false; }
        if (_harvestQueueUntil > 0)
        {
            if (now < _harvestQueueUntil) return true;
            StopHarvestWait("等待超时，恢复普通求解");
            return false;
        }
        if (_harvestWaitUsed || _forecastOnly) return false;
        if (now < Current.Now || now - Current.Now > Current.Gcd * 1000)
        { StopHarvestWait("快照时间异常", failed: true); return false; }
        _harvestWaitUsed = true;
        _harvestQueueUntil = now + (long)((Current.GcdLeft + ReaperBurstRecovery.HarvestMaxExtraWaitSeconds) * 1000);
        DeferHarvestQueue = true;
        HarvestWaitNote(now, $"短等大丰收：GCD={Current.GcdLeft:F3} 祭祀={Current.Bloodsown:F3} 最多额外={ReaperBurstRecovery.HarvestMaxExtraWaitSeconds:F1}s {unavailable}");
        return true;
    }

    internal void StopHarvestWait(string reason, bool failed = false)
    {
        _harvestWaitUsed |= failed;
        var active = _harvestQueueUntil > 0;
        _harvestQueueUntil = 0; DeferHarvestQueue = false;
        if (active) HarvestWaitNote(Current.Now, $"大丰收短等结束：{reason}");
    }

    private void HarvestWaitNote(long now, string text)
    {
        if (_forecastOnly) return;
        try { ReaperBattleData.Instance.DebugLog.Note(now, text); } catch (Exception) { }
    }

    private static bool HarvestConflicts(uint id) => id is ReaperSkill.夜游魂衣 or ReaperSkill.暴食
        or ReaperSkill.隐匿挥割 or ReaperSkill.绞决爪 or ReaperSkill.缢杀爪;

    // Debug也会调用Allows，保持只读；实时出招与Update负责结束等待。
    internal bool AllowsDuringHarvestWait(uint id, long now) => !DeferHarvestQueue || !CanWaitForHarvest
        || _harvestQueueUntil > 0 && now >= _harvestQueueUntil || !HarvestConflicts(id);

    private (float? Communio, float? Perfectio) EstimateBurstFinish(ReaperState s)
    {
        if (!s.HasTiming || s.Now < _shroudSyncUntil) return (null, null);
        var communioAt = s.GcdLeft + Math.Max(0, s.Lemure - 1) * s.ReapGcd + s.CommunioCast;
        var shroudEnd = s.GcdLeft + Math.Max(0, s.Lemure - 1) * s.ReapGcd + s.CommunioGcd;
        if (s.CastingCommunio || s.LastGcd == ReaperSkill.团契
            && s.GcdElapsed >= s.CommunioCast && s.GcdElapsed < s.CommunioCast + 0.25f)
        {
            communioAt = Math.Max(0, s.CommunioCast - s.GcdElapsed);
            shroudEnd = s.GcdLeft;
        }

        var secondShroud = 4 * s.ReapGcd + s.CommunioGcd;
        (float Communio, float Perfectio) SecondAt(float start) =>
            (start + 4 * s.ReapGcd + s.CommunioCast, start + secondShroud);
        switch (Phase)
        {
            case ReaperBurstPhase.第一附体 when s.Enshrouded > 0:
                if (!_firstDot && !_skipFirstDot) shroudEnd += s.Gcd;
                var harvestAt = ReaperBurstRecovery.HarvestAt(s with { GcdLeft = shroudEnd });
                return SecondAt(harvestAt + s.Gcd);
            case ReaperBurstPhase.大丰收衔接:
                if (_harvestUsed || s.FreeEnshroud > 0) return SecondAt(s.GcdLeft);
                return SecondAt(ReaperBurstRecovery.HarvestAt(s) + s.Gcd);
            case ReaperBurstPhase.第二附体 when s.Enshrouded > 0:
            case ReaperBurstPhase.起手 when _openerStep == 7 && s.Enshrouded > 0:
                return (communioAt, shroudEnd);
            case ReaperBurstPhase.收尾 when s.Perfectio > 0:
            case ReaperBurstPhase.起手 when _openerStep == 8 && s.Perfectio > 0:
                return (null, s.GcdLeft);
            default:
                return (null, null);
        }
    }

    private void ObserveQts(ReaperState s)
    {
        var ending = s.WindowActive && s.WindowLeft <= 2 * s.Gcd;
        var rising = _qtKnown && ((!_circleQt && s.CircleQt) || (!_enshroudQt && s.EnshroudQt));
        if (_coordinateUntil > s.Now && (s.CircleQt && s.EnshroudQt || s.CircleLeft > 0 || s.Enshrouded > 0 || ending))
            _coordinateUntil = 0;
        if (rising && s.InCombat && s.HasTiming && s.CircleCd <= s.Gcd && s.CircleLeft <= 0 && s.Enshrouded <= 0
            && !ending && !(s.CircleQt && s.EnshroudQt) && s.Now >= _coordinateCooldown)
        {
            _coordinateUntil = s.Now + (long)(s.Gcd * 1000);
            _coordinateCooldown = _coordinateUntil + (long)(s.Gcd * 1000);
        }
        _circleQt = s.CircleQt; _enshroudQt = s.EnshroudQt; _qtKnown = true;
    }

    public void ObserveAction(uint id)
    {
        if (id == ReaperSkill.神秘环)
        { StopHarvestWait("新一轮神秘环"); _harvestWaitUsed = false; }
        RecoveryRoute = ReaperBurstRecovery.AfterAction(RecoveryRoute, id);
        if (id is < 24373 or > 36973) return;
        var gcd = IsOrdinaryGcd(id) || id is ReaperSkill.虚无收割 or ReaperSkill.交错收割 or ReaperSkill.团契 or ReaperSkill.大丰收 or ReaperSkill.完人;
        if (gcd)
        {
            StopHarvestWait(id == ReaperSkill.大丰收 ? "大丰收已执行" : "已执行其他GCD");
            if (_lastRecordedGcd == id && Current.Now - _lastRecordedGcdAt < 1200) return;
            _lastRecordedGcd = id; _lastRecordedGcdAt = Current.Now;
        }
        _progressAt = Current.Now;
        _recent.Enqueue($"{Current.Now}: {id} ({Phase})");
        if (_recent.Count > 24) _recent.Dequeue();
        if (Phase == ReaperBurstPhase.起手)
        {
            if (_openerStep == 0 && id == ReaperSkill.死亡之影) _openerStep = 1;
            else if (_openerStep == 1 && id == ReaperSkill.灵魂切割) _openerStep = 2;
            else if (_openerStep == 2 && id == ReaperSkill.神秘环) _openerStep = 3;
            else if (_openerStep == 3 && id == ReaperSkill.暴食) { _openerStep = 4; _executions = 0; }
            else if (_openerStep == 4 && IsPositional(id) && ++_executions >= 2) _openerStep = 5;
            else if (_openerStep == 5 && id == ReaperSkill.大丰收) _openerStep = 6;
            else if (_openerStep == 6 && id == ReaperSkill.夜游魂衣) _openerStep = 7;
            else if (_openerStep == 7 && id == ReaperSkill.团契) { _openerStep = 8; _phaseAt = Current.Now; }
            else if (_openerStep == 8 && id == ReaperSkill.完人) SetPhase(ReaperBurstPhase.非爆发期);
            return;
        }
        if (Phase == ReaperBurstPhase.准备)
        {
            if (id == ReaperSkill.夜游魂衣) SetPhase(ReaperBurstPhase.第一附体);
            else if (IsOrdinaryGcd(id)) _prepGcds++;
        }
        else if (Phase == ReaperBurstPhase.第一附体)
        {
            if (id == ReaperSkill.死亡之影) { _firstDot = true; _skipFirstDot = false; }
            if (id == ReaperSkill.团契) SetPhase(ReaperBurstPhase.大丰收衔接);
        }
        else if (Phase == ReaperBurstPhase.大丰收衔接)
        {
            if (id == ReaperSkill.大丰收) _harvestUsed = true;
            if (id == ReaperSkill.夜游魂衣 && _harvestUsed) SetPhase(ReaperBurstPhase.第二附体);
        }
        else if (Phase == ReaperBurstPhase.第二附体 && id == ReaperSkill.团契)
            SetPhase(ReaperBurstPhase.收尾);
        else if (Phase == ReaperBurstPhase.收尾)
        {
            if (id == ReaperSkill.完人) _perfectioDone = true;
            if (id is ReaperSkill.增盈切割 or ReaperSkill.地狱切割 or ReaperSkill.切割) _finishComboDone = true;
            if (id == ReaperSkill.暴食) SetPhase(ReaperBurstPhase.非爆发期);
        }
    }

    private static bool IsPositional(uint id) => id is ReaperSkill.绞决 or ReaperSkill.缢杀 or ReaperSkill.绞决处刑 or ReaperSkill.缢杀处刑;
    private static bool IsOrdinaryGcd(uint id) => id is ReaperSkill.切割 or ReaperSkill.增盈切割 or ReaperSkill.地狱切割
        or ReaperSkill.死亡之影 or ReaperSkill.灵魂切割 or ReaperSkill.收获月 or ReaperSkill.勾刃 || IsPositional(id);

    private void Reconcile(ReaperState s)
    {
        if (Phase == ReaperBurstPhase.起手)
        {
            if (_openerStep == 2 && (s.CircleLeft > 0 || s.CircleCd > 60)) _openerStep = 3;
            if (_openerStep == 3 && s.Reavers > 0) { _openerStep = 4; _executions = 0; }
            if (_openerStep == 4 && _previous.Reavers > 0 && s.Reavers == 0) _openerStep = 5;
            if (_openerStep == 5 && s.FreeEnshroud > 0) _openerStep = 6;
            if (_openerStep == 6 && s.Enshrouded > 0) _openerStep = 7;
        }
        // Buff/量谱校正漏事件；不因为返回过PAction就认为它已经成功。
        if (Phase == ReaperBurstPhase.准备 && s.Enshrouded > 0) SetPhase(ReaperBurstPhase.第一附体);
        if (Phase == ReaperBurstPhase.第一附体)
        {
            if (s.LastGcd == ReaperSkill.死亡之影 && s.Lemure == 4 && s.Enshrouded > 0
                && Math.Abs(s.GcdLeft + s.GcdElapsed - s.Gcd) < 0.1f)
            { _firstDot = true; _skipFirstDot = false; }
            if (_previous.Enshrouded > 0 && s.Enshrouded <= 0)
            {
                if (s.LastGcd == ReaperSkill.团契) SetPhase(ReaperBurstPhase.大丰收衔接);
                else Cancel("第一附体中断");
            }
        }
        if (Phase == ReaperBurstPhase.大丰收衔接)
        {
            if (s.FreeEnshroud > 0) _harvestUsed = true;
            if (_harvestUsed && s.Enshrouded > 0) SetPhase(ReaperBurstPhase.第二附体);
        }
        if (Phase == ReaperBurstPhase.第二附体 && _previous.Enshrouded > 0 && s.Enshrouded <= 0)
        {
            if (s.LastGcd == ReaperSkill.团契 || s.Perfectio > 0) SetPhase(ReaperBurstPhase.收尾);
            else Cancel("第二附体中断");
        }
        if (Phase == ReaperBurstPhase.收尾 && s.LastGcd == ReaperSkill.完人) _perfectioDone = true;
        if (s.Lemure != _previous.Lemure || s.Reavers != _previous.Reavers || s.LastGcd != _previous.LastGcd)
            _progressAt = s.Now;
    }

    private void BuildPlan(ReaperState s)
    {
        if (Phase == ReaperBurstPhase.起手) { BuildOpener(s); return; }
        if (Phase == ReaperBurstPhase.准备)
        {
            if (s.CircleCd <= 0) { Cancel("准备未完成，神秘环优先转简单爆发"); return; }
            if (!s.Melee || s.Shroud < 50) { Cancel("双附体准备条件丢失"); return; }
            if (s.DeathDesign < 10) GcdAction = ReaperSkill.死亡之影;
            else if (s.ComboAtRisk(s.DoubleDuration)) GcdAction = s.ComboNext;
            else if (s.SliceQt && s.SliceCharges >= 1 && s.Soul <= 50) GcdAction = ReaperSkill.灵魂切割;
            else GcdAction = Combo(s);
            var lastCircleWeave = s.GcdLeft + s.ReapGcd + s.Gcd - 0.65f;
            if (_prepGcds >= 2 && s.CanEnshroud && s.DeathDesign >= 10 && !s.ComboAtRisk(s.DoubleDuration)
                && s.CircleCd <= lastCircleWeave) OffGcdAction = ReaperSkill.夜游魂衣;
            return;
        }
        if (Phase is ReaperBurstPhase.第一附体 or ReaperBurstPhase.第二附体)
        {
            if (s.Enshrouded <= 0) return; // 能力技生效后的短暂量谱同步，由阶段超时统一兜底。
            if (Phase == ReaperBurstPhase.第一附体 && !_firstDot)
            {
                // 开环成功前按实况重算；成功后不再因为这个插入位置已过而补回填充。
                if (s.CircleLeft <= 0 && s.CircleCd < 60 && s.Now >= _shroudSyncUntil)
                {
                    var firstReapAt = float.NaN;
                    if (s.Lemure == 5) firstReapAt = s.GcdLeft;
                    else if (s.Lemure == 4 && Math.Abs(s.GcdLeft + s.GcdElapsed - s.ReapGcd) < 0.1f)
                        firstReapAt = -s.GcdElapsed;
                    _skipFirstDot = ReaperResources.CanSkipFirstDesign(s, firstReapAt, out _);
                }
                if (!s.DotQt && !_skipFirstDot) { Replan("烙印填充关闭，按当前权限重算"); return; }
                if (_skipFirstDot) Reason = "烙印充足，首轮收割后直接开环";
            }
            if (Phase == ReaperBurstPhase.第一附体 && !_firstDot && !_skipFirstDot && s.Lemure == 4)
                GcdAction = ReaperSkill.死亡之影;
            else GcdAction = ShroudGcd(s);
            var circleWeave = _firstDot ? s.FastCircle || s.GcdElapsed >= s.Gcd / 2 || s.LastGcd != ReaperSkill.死亡之影
                : _skipFirstDot && s.Lemure == 4 && s.GcdElapsed >= 0.65f && s.GcdLeft > 0.65f;
            if (Phase == ReaperBurstPhase.第一附体 && circleWeave && s.CircleQt && s.CircleLeft <= 0 && s.CircleCd <= 0)
                OffGcdAction = ReaperSkill.神秘环;
            else OffGcdAction = ShroudOffGcd(s, Phase == ReaperBurstPhase.第一附体 && (s.CircleLeft <= 0 || s.Lemure > 2));
            return;
        }
        if (Phase == ReaperBurstPhase.大丰收衔接)
        {
            if (s.CanHarvest || ReaperBurstRecovery.HarvestNextGcd(s)) GcdAction = ReaperSkill.大丰收;
            else if (_harvestUsed && s.CanEnshroud)
            {
                if (s.ComboAtRisk(ReaperResources.EnshroudComboDelay(s))) GcdAction = s.ComboNext;
                else OffGcdAction = ReaperSkill.夜游魂衣;
            }
            else if (s.Bloodsown <= 0 && !_harvestUsed && s.SacrificeStacks == 0 && s.Now - _phaseAt > 1500)
                Cancel("没有可用大丰收，转剩余团辅爆发");
            return;
        }
        if (Phase == ReaperBurstPhase.收尾)
        {
            if (s.ComboAtRisk(s.Perfectio > 0 ? s.PerfectioGcd : s.Gcd)) GcdAction = s.ComboNext;
            else if (ReaperResources.AllowsPerfectio(s))
                GcdAction = ReaperSkill.完人;
            else if (!_perfectioDone && s.Perfectio <= 0 && s.Now - _phaseAt < 1000) return;
            else if (!_finishComboDone && s.ComboNext != 0 && s.Melee) GcdAction = s.ComboNext;
            else if (s.GluttonyQt && s.GluttonyCd <= 0 && s.Soul >= 50 && !s.Locked)
                OffGcdAction = ReaperSkill.暴食;
            else if (s.GluttonyQt && s.GluttonyCd <= 0 && s.Soul < 50 && s.SliceQt && s.SliceCharges >= 1 && !s.Locked)
                GcdAction = ReaperSkill.灵魂切割;
            else SetPhase(ReaperBurstPhase.非爆发期);
        }
    }

    private void BuildOpener(ReaperState s)
    {
        switch (_openerStep)
        {
            case 0: GcdAction = ReaperSkill.死亡之影; break;
            case 1: GcdAction = ReaperSkill.灵魂切割; break;
            case 2: OffGcdAction = ReaperSkill.神秘环; break;
            case 3:
                if (s.Soul >= 50) OffGcdAction = ReaperSkill.暴食;
                else if (s.Now - _progressAt > 1200) Cancel("起手红不足");
                break;
            case 4:
                if (s.Reavers > 0 && s.Melee) GcdAction = ReaperSkill.缢杀;
                break;
            case 5:
                if (s.CanHarvest || ReaperBurstRecovery.HarvestNextGcd(s)) GcdAction = ReaperSkill.大丰收;
                else if (s.Bloodsown > 0) GcdAction = Combo(s);
                else if (s.SacrificeStacks == 0 && s.Now - _progressAt > 1500) Cancel("起手没有祭品");
                break;
            case 6:
                if (s.CanEnshroud && s.FreeEnshroud > 0) OffGcdAction = ReaperSkill.夜游魂衣;
                break;
            case 7: GcdAction = ShroudGcd(s); OffGcdAction = ShroudOffGcd(s, false); break;
            case 8:
                if (s.Perfectio > 0) GcdAction = ReaperSkill.完人;
                else if (s.Now - _phaseAt > 1500) SetPhase(ReaperBurstPhase.非爆发期);
                break;
        }
    }

    private void BuildNormal(ReaperState s)
    {
        var inBuff = s.CircleLeft > 0;
        var harvestPriority = ReaperBurstRecovery.PrioritizeHarvestReward(s);
        if (IsCoordinating) Reason = "等待QT组合完成（最多一个GCD）";
        else if (inBuff && !IsSimple) Reason = "按剩余神秘环兑现资源";
        else if (IsSimple) Reason = FallbackReason;
        else if (!IsSimple) Reason = "非爆发期";
        if (s.Enshrouded > 0)
        {
            GcdAction = ShroudGcd(s); OffGcdAction = ShroudOffGcd(s, false);
            if (GcdAction != ReaperSkill.收获月 && ReaperResources.NeedsDesignInShroud(s))
            { GcdAction = ReaperSkill.死亡之影; Reason = "烙印不足以覆盖附体收尾"; }
            if (s.CircleQt && s.CircleCd <= 0 && !IsCoordinating) OffGcdAction = ReaperSkill.神秘环;
            return;
        }
        if (s.Reavers > 0) GcdAction = s.Melee ? ReaperSkill.缢杀 : 0;
        else if (s.ComboAtRisk(s.HasTiming ? s.Gcd : 3)) GcdAction = s.ComboNext;
        else if (s.Perfectio > 0 && s.ComboAtRisk(s.PerfectioGcd)) GcdAction = s.ComboNext;
        else if (inBuff && s.CircleLeft <= Math.Max(s.Gcd, 2) && ReaperResources.AllowsPerfectio(s)) GcdAction = ReaperSkill.完人;
        else if (ReaperResources.ShouldRefreshBeforeGluttony(s))
        { GcdAction = ReaperSkill.死亡之影; Reason = "暴食前补烙印，覆盖处刑后的续印"; }
        else if (ReaperResources.ShouldRefreshDeathDesign(s))
        { GcdAction = ReaperSkill.死亡之影; Reason = "提前续死亡烙印"; }
        else if (ReaperResources.AllowsPerfectio(s)) GcdAction = ReaperSkill.完人;
        else if ((s.CanHarvest || ReaperBurstRecovery.HarvestNextGcd(s))
            && (s.EnshroudQt || s.SacrificeLeft <= Math.Max(3, s.Gcd + 1)))
        {
            GcdAction = ReaperSkill.大丰收;
            if (harvestPriority) Reason = "优先大丰收，衔接免费附体";
        }
        else if (ReaperResources.IsWindowClosing(s) && s.WindowLeft <= Math.Max(2 * s.Gcd, 5)
            && ReaperResources.AllowsHarvestMoon(s))
            GcdAction = ReaperSkill.收获月;
        if (GcdAction == 0 && s.Perfectio > 0 && s.FarPerfectioQt && s.Melee && !ReaperResources.AllowsPerfectio(s))
            Reason = "近战保留完人，继续其他可用技能";

        if (s.CircleQt && s.CircleCd <= 0 && !IsCoordinating)
        {
            OffGcdAction = ReaperSkill.神秘环;
            return;
        }
        if (s.Locked) return;
        if (GcdAction is ReaperSkill.大丰收 or ReaperSkill.完人 or ReaperSkill.死亡之影) return;
        if (harvestPriority && s.FreeEnshroud > 0 && s.Melee && !IsCoordinating)
        {
            if (s.ComboAtRisk(ReaperResources.EnshroudComboDelay(s))) GcdAction = s.ComboNext;
            else if (s.DotQt && s.DeathDesign < s.GcdLeft + s.SingleDuration + 1)
                GcdAction = ReaperSkill.死亡之影;
            else if (s.CanEnshroud)
            {
                OffGcdAction = ReaperSkill.夜游魂衣;
                Reason = s.ComboNext == 0 ? "优先兑现大丰收的免费附体"
                    : $"优先免费附体，预计{s.GcdLeft + ReaperResources.EnshroudComboDelay(s):F2}s后续连击";
            }
            return;
        }
        if (s.CanEnshroud && s.Melee && !IsCoordinating)
        {
            var why = "";
            var hold = !IsSimple && ReaperResources.ShouldHoldEnshroud(s, out why);
            if (why.Length > 0) Reason = why;
            var chooseEnshroud = !hold && (!inBuff || ReaperResources.EnshroudWinsRemainingBuff(s))
                && (s.FreeEnshroud > 0 || !s.GluttonyQt || s.GluttonyCd > 0 || s.Soul < 50 || inBuff);
            var sliceWillCap = !inBuff && s.SliceQt && s.SliceCharges >= 1 && s.HasTiming
                && (s.FreeEnshroud <= 0 || s.FreeEnshroud > s.GcdLeft + 2 * s.Gcd + 1)
                && (2 - s.SliceCharges) * s.SliceRecast <= s.GcdLeft + s.SingleDuration + s.Gcd;
            if (chooseEnshroud && sliceWillCap && s.Soul > 50 && s.BloodQt
                && !ReaperResources.ShouldHoldBlood(s, out _))
            {
                chooseEnshroud = false;
                Reason = "先消费红，为即将满层的灵魂切割腾空间";
            }
            if (chooseEnshroud && s.ComboAtRisk(ReaperResources.EnshroudComboDelay(s))) { GcdAction = s.ComboNext; return; }
            if (chooseEnshroud && s.DotQt && s.HasTiming && s.DeathDesign < s.GcdLeft + s.SingleDuration + 1)
            {
                GcdAction = ReaperSkill.死亡之影;
                return;
            }
            if (chooseEnshroud && sliceWillCap && s.Soul <= 50)
            {
                GcdAction = ReaperSkill.灵魂切割;
                Reason = "普通附体前先用灵魂切割，避免充能溢出";
                return;
            }
            if (chooseEnshroud) OffGcdAction = ReaperSkill.夜游魂衣;
        }
        if (OffGcdAction == 0 && s.GluttonyQt && s.GluttonyCd <= 0 && s.Soul >= 50)
        {
            if (s.ComboAtRisk(2 * s.Gcd)) { GcdAction = s.ComboNext; return; }
            OffGcdAction = ReaperSkill.暴食;
        }
        if (OffGcdAction == 0 && s.BloodQt && s.Melee && s.Soul >= 50 && !s.ComboAtRisk(s.Gcd))
        {
            if (ReaperResources.ShouldHoldBlood(s, out var why)) Reason = why;
            else OffGcdAction = ReaperSkill.隐匿挥割;
        }
    }

    private static uint Combo(ReaperState s) => s.ComboNext != 0 ? s.ComboNext : ReaperSkill.切割;
    private static uint ShroudGcd(ReaperState s)
    {
        if (s.Enshrouded <= 0 || s.Lemure <= 0) return 0;
        if (s.Lemure > 1 || s.Level < 90) return s.Melee ? ReaperSkill.虚无收割 : s.CanHarvestMoonForRange ? ReaperSkill.收获月 : 0;
        if (!s.Moving && s.Distance <= GameData.GetCurrentAttackRange(25)) return ReaperSkill.团契;
        return s.CanHarvestMoonForRange ? ReaperSkill.收获月 : 0;
    }
    private static uint ShroudOffGcd(ReaperState s, bool holdSacrifice)
    {
        if (s.Enshrouded <= 0) return 0;
        if (s.Void >= 2 && s.Melee) return ReaperSkill.夜游魂切割;
        if (s.Oblatio > 0 && (!holdSacrifice || s.Lemure <= 1) && s.Distance <= GameData.GetCurrentAttackRange(25))
            return ReaperSkill.祭性;
        return 0;
    }

    public bool Allows(uint id)
    {
        if (IsOpener) return id == GcdAction || id == OffGcdAction;
        if (!AllowsResource(id)) return false;
        if (RulesDisabled) return true;
        var s = Current;
        if (id == ReaperSkill.神秘环) return !IsCoordinating && (!IsPlanned || OffGcdAction == id);
        if (id == ReaperSkill.夜游魂衣) return !IsCoordinating && OffGcdAction == id;
        if (id == ReaperSkill.暴食) return OffGcdAction == id;
        if (id is ReaperSkill.隐匿挥割 or ReaperSkill.绞决爪 or ReaperSkill.缢杀爪) return OffGcdAction == ReaperSkill.隐匿挥割;
        if (id == ReaperSkill.大丰收) return (!IsPlanned || GcdAction == id) && s.CanHarvest
            && (s.IsDump || s.EnshroudQt || s.SacrificeLeft <= Math.Max(3, s.Gcd + 1));
        if (id == ReaperSkill.祭性 && Phase == ReaperBurstPhase.第一附体
            && (s.CircleLeft <= 0 || s.Lemure > 2) && s.Lemure > 1) return false;
        return true;
    }

    public bool AllowsResource(uint id, bool fallback = false)
    {
        if (IsOpener) return id == GcdAction || id == OffGcdAction;
        if (!AllowsDuringHarvestWait(id, _forecastOnly ? Current.Now : Environment.TickCount64)) return false;
        var s = Current;
        if (!s.WindowActive) return true;
        if (id == ReaperSkill.夜游魂衣 && (s.WindowLeft > 0 ? s.WindowLeft : s.WindowLimit) <= 0.8f) return false;
        if (!fallback && !RulesDisabled && id == OffGcdAction) return true;
        if (id == ReaperSkill.夜游魂衣) return s.FreeEnshroud > 0 || s.Shroud - 50 >= s.GoalShroud;
        if (id is ReaperSkill.暴食 or ReaperSkill.隐匿挥割 or ReaperSkill.绞决爪 or ReaperSkill.缢杀爪)
            return s.Soul - 50 >= s.GoalSoul;
        return true;
    }
}
