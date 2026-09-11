using System.Collections.Concurrent;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper;

// 90–99级的进度与预测完全独立。共享资源函数仅使用归一化后的时序，不使用100级收益表。
internal sealed class ReaperLevel90Planner : IReaperPlanner
{
    private readonly ConcurrentQueue<(ulong Source, uint Id, uint Sequence, long At)> _events = new();
    private readonly Queue<(uint Sequence, uint Id)> _seen = new();
    private readonly Queue<string> _recent = new();
    private readonly ReaperLevel90Projection _projection = new();
    private ReaperState _previous;
    private long _phaseAt, _progressAt, _retryAt, _simpleUntil, _resetAt;
    private long _coordinateUntil, _coordinateCooldown, _harvestUntil, _sliceUntil, _shroudSyncUntil;
    private bool _qtKnown, _circleQt, _enshroudQt, _harvestWaitUsed;
    private bool _firstDot, _skipFirstDot, _harvestUsed, _finishCombo;
    private int _prepGcds, _openerStep, _positionals;
    private uint _lastGcd;
    private long _lastGcdAt;
    private readonly bool _forecastOnly;

    public ReaperLevel90Planner(bool forecastOnly = false) => _forecastOnly = forecastOnly;
    public ReaperState Current { get; private set; }
    public ReaperBurstPhase Phase { get; private set; }
    public string Reason { get; private set; } = "非爆发期";
    public string ModeName => Current.IsDump ? "90级倾泻" : $"90级{(IsSimple ? "兜底" : Phase.ToString())}";
    public uint GcdAction { get; private set; }
    public uint OffGcdAction { get; private set; }
    public int ForecastGreen { get; private set; } = -1;
    public string ForecastGreenText => ForecastGreen >= 0 ? ForecastGreen.ToString()
        : !Current.HasTiming ? "时序未读取" : !Current.CircleQt ? "120关闭" : "当前无需预测";
    public float? BurstFinishIn => null;
    public float? BurstCommunioIn { get; private set; }
    public string FallbackReason { get; private set; } = "";
    public string ProjectionDebug => _projection.Debug;
    public IEnumerable<string> RecentActions => _recent;
    public ReaperBurstRoute RecoveryRoute => ReaperBurstRoute.当前安排;
    public bool IsPlanned => Phase != ReaperBurstPhase.非爆发期;
    public bool IsOpener => Phase == ReaperBurstPhase.起手;
    public bool IsCoordinating => Current.Now < _coordinateUntil;
    public bool IsSimple => Current.Now < _simpleUntil;
    public bool RulesDisabled { get; private set; }
    public bool DeferSliceQueue { get; private set; }
    public bool DeferHarvestQueue { get; private set; }

    internal ReaperLevel90Planner Fork() => new(true)
    {
        Current = Current, _previous = _previous, Phase = Phase, _phaseAt = _phaseAt,
        _progressAt = _progressAt, _retryAt = _retryAt, _simpleUntil = _simpleUntil,
        _firstDot = _firstDot, _skipFirstDot = _skipFirstDot, _harvestUsed = _harvestUsed,
        _prepGcds = _prepGcds, _finishCombo = _finishCombo, _openerStep = _openerStep,
        _positionals = _positionals, _lastGcd = _lastGcd, _lastGcdAt = _lastGcdAt,
        _qtKnown = _qtKnown, _circleQt = _circleQt, _enshroudQt = _enshroudQt
    };

    public void EnqueueAction(ulong source, uint id, uint sequence) =>
        _events.Enqueue((source, id, sequence, Environment.TickCount64));

    public void Reset(string reason = "重置")
    {
        _events.Clear(); _seen.Clear(); _recent.Clear(); _projection.Clear();
        Current = _previous = default;
        Phase = ReaperBurstPhase.非爆发期;
        GcdAction = OffGcdAction = _lastGcd = 0;
        _phaseAt = _progressAt = _retryAt = _simpleUntil = _lastGcdAt = 0;
        _coordinateUntil = _coordinateCooldown = _harvestUntil = _sliceUntil = _shroudSyncUntil = 0;
        _qtKnown = _circleQt = _enshroudQt = _harvestWaitUsed = false;
        _firstDot = _skipFirstDot = _harvestUsed = _finishCombo = false;
        _prepGcds = _openerStep = _positionals = 0;
        RulesDisabled = DeferHarvestQueue = DeferSliceQueue = false;
        ForecastGreen = -1; BurstCommunioIn = null; FallbackReason = "";
        Reason = reason; _resetAt = Environment.TickCount64;
    }

    public void Replan(string reason)
    {
        Phase = ReaperBurstPhase.非爆发期;
        _firstDot = _skipFirstDot = _harvestUsed = _finishCombo = false;
        _prepGcds = 0;
        GcdAction = OffGcdAction = 0;
        BurstCommunioIn = null;
        StopHarvestWait(reason);
        _projection.Clear(); Reason = reason;
    }
    public void Cancel(string reason)
    {
        Replan(reason); FallbackReason = reason;
        _simpleUntil = Current.Now + 3000; _retryAt = Current.Now + 5000;
    }
    public void WindowChanged(string reason) { _projection.Clear(); StopHarvestWait(reason); Reason = reason; }
    public void DisableRules() { Cancel("90级规划异常，使用基础求解"); RulesDisabled = true; }
    public void NoteDumpFallback(uint id, bool off) => Reason = $"倾泻兜底：{id}";
    public void NoteOpenerBlocked(uint id, uint? status) => FallbackReason = $"起手等待{id}，状态{status}";

    public void BeginOpener(ReaperState state)
    {
        if (!ReaperLevelRules.UsesLevel90(state.Level) || !state.Alive) return;
        Reset("90级标准起手"); Current = ReaperLevelRules.Normalize(state);
        SetPhase(ReaperBurstPhase.起手);
    }
    private static ReaperState OpenerState(ReaperState s) => s with
    { CircleQt = true, EnshroudQt = true, GluttonyQt = true, SliceQt = true, DotQt = true, DumpQt = false, WindowActive = false };

    private void SetPhase(ReaperBurstPhase phase)
    { Phase = phase; _phaseAt = _progressAt = Current.Now; }

    public void Update(ReaperState state, bool evaluateResources = false)
    {
        var s = ReaperLevelRules.Normalize(state);
        Current = s; RulesDisabled = false;
        GcdAction = OffGcdAction = 0; ForecastGreen = -1;
        BurstCommunioIn = null; DeferHarvestQueue = DeferSliceQueue = false;
        if (!ReaperLevelRules.UsesLevel90(s.Level) || !s.Alive || !s.InCombat)
        {
            if (_previous.InCombat || IsPlanned) Reset(s.Alive ? "脱战/等级变化" : "死亡/未加载");
            Current = s; _previous = s; return;
        }
        if (_previous.Alive && (s.Level != _previous.Level || s.PlayerId != _previous.PlayerId))
        { Reset("等级或角色变化"); Current = s; }
        while (_events.TryDequeue(out var effect))
        {
            if (effect.Source != s.PlayerId || effect.At < _resetAt || _seen.Contains((effect.Sequence, effect.Id))) continue;
            _seen.Enqueue((effect.Sequence, effect.Id)); if (_seen.Count > 32) _seen.Dequeue();
            ObserveAction(effect.Id);
        }
        if (s.Enshrouded <= 0 || s.CastingCommunio || s.Lemure != _previous.Lemure) _shroudSyncUntil = 0;
        else if (_previous.Enshrouded > 0 && s.GcdElapsed + .1f < _previous.GcdElapsed) _shroudSyncUntil = s.Now + 500;
        if (s.LastGcd != 0 && (s.LastGcd != _previous.LastGcd || s.GcdElapsed + .1f < _previous.GcdElapsed)
            && (s.LastGcd != ReaperSkill.团契 || s.Enshrouded <= 0)
            && !(s.LastGcd == ReaperSkill.死亡之影 && s.Enshrouded > 0 && Math.Abs(s.GcdLeft + s.GcdElapsed - s.Gcd) > .1f))
            ObserveAction(s.LastGcd);
        Reconcile(s);
        if (IsPlanned && s.Now - _progressAt > Math.Max(9000, 3 * s.Gcd * 1000 + 2000)) Cancel("计划未推进，按实况继续");
        if (IsOpener)
        {
            if (!s.HasTarget || s.Now - _phaseAt > 40000) Cancel("起手中断");
            else { Reason = "90级标准起手"; BuildOpener(OpenerState(s)); UpdateQueues(OpenerState(s)); _previous = s; return; }
        }
        if (Phase == ReaperBurstPhase.大丰收衔接 && !s.CircleQt && !_harvestUsed)
            Replan("120关闭，跳过大丰收衔接");
        if (s.IsDump != _previous.IsDump) Replan("倾泻模式变化");
        ObserveQts(s);
        if (_previous.Alive && (s.WindowActive != _previous.WindowActive || s.WindowVersion != _previous.WindowVersion
            || s.FastCircle != _previous.FastCircle)) WindowChanged("窗口或快速神秘环变化");
        if (IsPlanned && (!s.HasTiming || !s.HasTarget || s.TargetId != _previous.TargetId
            || !s.EnshroudQt && s.Enshrouded <= 0 || !s.CircleQt && s.CircleLeft <= 0 || s.IsDump))
            Replan("目标、时序或QT变化");
        if (!s.HasTarget) { StopHarvestWait("目标不可用"); _previous = s; return; }

        if (s.HasTiming && s.CircleQt && s.CircleLeft <= 0 && s.Shroud >= 50 && !s.Locked && s.FreeEnshroud <= 0)
            ForecastGreen = ReaperResources.ForecastShroud(s, true);
        if (!IsPlanned && !s.IsDump && !IsSimple && !IsCoordinating && s.Now >= _retryAt
            && s.HasTiming && s.Melee && s.CircleQt && s.EnshroudQt && s.DotQt && !s.Locked
            && s.Shroud >= 50 && s.FreeEnshroud <= 0 && s.CircleLeft <= 0 && s.CircleCd > 0
            && s.CircleCd <= s.PreparationLead
            && (!s.WindowActive || s.WindowLeft > s.CircleCd + 20 || ReaperResources.ForecastPreparation(s, false).Ready))
        {
            _prepGcds = 0; _firstDot = _skipFirstDot = _harvestUsed = _finishCombo = false;
            SetPhase(ReaperBurstPhase.准备);
        }
        Reason = IsCoordinating ? "等待QT组合完成（最多一个GCD）" : Phase.ToString();
        if (IsPlanned)
        {
            BuildBurst(s);
            BurstCommunioIn = EstimateCommunio(s);
            if (BurstCommunioIn.HasValue && (s.CircleLeft > 0 && BurstCommunioIn + .15f >= s.CircleLeft
                || s.WindowActive && BurstCommunioIn + .15f >= s.WindowLeft))
                Replan("双团契已无法覆盖，兑现当前爆发奖励");
        }
        if (!IsPlanned) BuildNormal(s);
        if (s.WindowActive && !IsOpener) ApplyReserve(s);
        if (!_forecastOnly && evaluateResources && s.HasTiming && !IsCoordinating && !IsOpener && !IsSimple)
        {
            var advice = _projection.Choose(this, s, GcdAction, OffGcdAction);
            if (advice.HasValue)
            {
                if (advice.Value.Off) OffGcdAction = advice.Value.Id; else GcdAction = advice.Value.Id;
                Reason = advice.Value.Reason;
            }
        }
        UpdateQueues(s); _previous = s;
    }

    private void ObserveQts(ReaperState s)
    {
        var ending = s.WindowActive && s.WindowLeft <= 2 * s.Gcd;
        if (s.CircleQt && s.EnshroudQt || s.CircleLeft > 0 || s.Enshrouded > 0 || ending || s.IsDump) _coordinateUntil = 0;
        var rising = _qtKnown && (!_circleQt && s.CircleQt || !_enshroudQt && s.EnshroudQt);
        if (rising)
        {
            _retryAt = _simpleUntil = 0; _projection.Clear();
            if (!s.IsDump && !ending && s.HasTiming && s.CircleCd <= s.PreparationLead && s.CircleLeft <= 0
                && s.Enshrouded <= 0 && !(s.CircleQt && s.EnshroudQt) && s.Now >= _coordinateCooldown)
            {
                _coordinateUntil = s.Now + (long)(s.Gcd * 1000);
                _coordinateCooldown = _coordinateUntil + (long)(s.Gcd * 1000);
            }
        }
        _circleQt = s.CircleQt; _enshroudQt = s.EnshroudQt; _qtKnown = true;
    }

    public void ObserveAction(uint id)
    {
        if (id == ReaperSkill.神秘环) { StopHarvestWait("新神秘环"); _harvestWaitUsed = false; }
        var gcd = IsGcd(id);
        if (gcd)
        {
            StopHarvestWait("已执行GCD");
            if (id == _lastGcd && Current.Now - _lastGcdAt < 1200) return;
            _lastGcd = id; _lastGcdAt = Current.Now;
        }
        _progressAt = Current.Now;
        if (!_forecastOnly)
        { _recent.Enqueue($"{Current.Now}: {id} ({Phase})"); if (_recent.Count > 24) _recent.Dequeue(); }
        if (IsOpener)
        {
            if (_openerStep == 0 && id == ReaperSkill.死亡之影) _openerStep = 1;
            else if (_openerStep == 1 && id == ReaperSkill.灵魂切割) _openerStep = 2;
            else if (_openerStep == 2 && id == ReaperSkill.神秘环) _openerStep = 3;
            else if (_openerStep == 3 && id == ReaperSkill.暴食) _openerStep = 4;
            else if (_openerStep == 4 && IsPositional(id) && ++_positionals >= 2) _openerStep = 5;
            else if (_openerStep == 5 && id == ReaperSkill.大丰收) _openerStep = 6;
            else if (_openerStep == 6 && id == ReaperSkill.夜游魂衣) _openerStep = 7;
            else if (_openerStep == 7 && id == ReaperSkill.团契) SetPhase(ReaperBurstPhase.非爆发期);
            return;
        }
        switch (Phase)
        {
            case ReaperBurstPhase.准备:
                if (id == ReaperSkill.夜游魂衣) SetPhase(ReaperBurstPhase.第一附体);
                else if (gcd) _prepGcds++;
                break;
            case ReaperBurstPhase.第一附体:
                if (id == ReaperSkill.死亡之影) { _firstDot = true; _skipFirstDot = false; }
                if (id == ReaperSkill.团契) SetPhase(ReaperBurstPhase.大丰收衔接);
                break;
            case ReaperBurstPhase.大丰收衔接:
                if (id == ReaperSkill.大丰收) _harvestUsed = true;
                if (id == ReaperSkill.夜游魂衣 && _harvestUsed) SetPhase(ReaperBurstPhase.第二附体);
                break;
            case ReaperBurstPhase.第二附体:
                if (id == ReaperSkill.团契) SetPhase(ReaperBurstPhase.收尾);
                break;
            case ReaperBurstPhase.收尾:
                if (id is ReaperSkill.切割 or ReaperSkill.增盈切割 or ReaperSkill.地狱切割) _finishCombo = true;
                if (id == ReaperSkill.暴食) SetPhase(ReaperBurstPhase.非爆发期);
                break;
        }
    }

    private void Reconcile(ReaperState s)
    {
        if (s.Lemure != _previous.Lemure || s.Reavers != _previous.Reavers || s.LastGcd != _previous.LastGcd) _progressAt = s.Now;
        if (IsOpener)
        {
            if (_openerStep == 2 && s.CircleCd > 60) _openerStep = 3;
            if (_openerStep == 3 && s.Reavers > 0) _openerStep = 4;
            if (_openerStep == 4 && _previous.Reavers > 0 && s.Reavers == 0) _openerStep = 5;
            if (_openerStep == 5 && s.FreeEnshroud > 0) _openerStep = 6;
            if (_openerStep == 6 && s.Enshrouded > 0) _openerStep = 7;
            if (_openerStep == 7 && _previous.Enshrouded > 0 && s.Enshrouded <= 0) SetPhase(ReaperBurstPhase.非爆发期);
        }
        if (Phase == ReaperBurstPhase.准备 && s.Enshrouded > 0) SetPhase(ReaperBurstPhase.第一附体);
        if (Phase == ReaperBurstPhase.大丰收衔接)
        {
            if (s.FreeEnshroud > 0) _harvestUsed = true;
            if (_harvestUsed && s.Enshrouded > 0) SetPhase(ReaperBurstPhase.第二附体);
        }
        if (_previous.Enshrouded > 0 && s.Enshrouded <= 0)
        {
            if (s.LastGcd == ReaperSkill.团契)
            {
                if (Phase == ReaperBurstPhase.第一附体) SetPhase(ReaperBurstPhase.大丰收衔接);
                else if (Phase == ReaperBurstPhase.第二附体) SetPhase(ReaperBurstPhase.收尾);
            }
            else if (IsPlanned) Cancel("附体被中断");
        }
    }

    private void BuildOpener(ReaperState s)
    {
        switch (_openerStep)
        {
            case 0: GcdAction = ReaperSkill.死亡之影; break;
            case 1: GcdAction = ReaperSkill.灵魂切割; break;
            case 2: OffGcdAction = ReaperSkill.神秘环; break;
            case 3: if (s.Soul >= 50) OffGcdAction = ReaperSkill.暴食; else Cancel("起手红不足"); break;
            case 4: if (s.Reavers > 0 && s.Melee) GcdAction = ReaperSkill.缢杀; break;
            case 5:
                if (s.CanHarvest || ReaperBurstRecovery.HarvestNextGcd(s)) GcdAction = ReaperSkill.大丰收;
                else if (s.Bloodsown > 0) GcdAction = Combo(s);
                else if (s.Now - _progressAt > 1500) Cancel("起手没有祭品");
                break;
            case 6: if (s.CanEnshroud && s.FreeEnshroud > 0) OffGcdAction = ReaperSkill.夜游魂衣; break;
            case 7: BuildShroud(s); BurstCommunioIn = s.ShroudFinishIn; break;
        }
    }

    private void BuildBurst(ReaperState s)
    {
        if (Phase == ReaperBurstPhase.准备)
        {
            if (s.CircleCd <= 0) { Cancel("准备未完成，按时开启神秘环"); return; }
            if (!s.Melee || s.Shroud < 50) { Cancel("双附体准备条件丢失"); return; }
            GcdAction = s.DeathDesign < 10 ? ReaperSkill.死亡之影 : s.ComboAtRisk(s.DoubleDuration) ? s.ComboNext
                : s.SliceQt && s.SliceCharges >= 1 && s.Soul <= 50 ? ReaperSkill.灵魂切割 : Combo(s);
            if (_prepGcds >= 2 && s.CanEnshroud && s.DeathDesign >= 10 && !s.ComboAtRisk(s.DoubleDuration)
                && s.CircleCd <= s.GcdLeft + s.ReapGcd + s.Gcd - .65f) OffGcdAction = ReaperSkill.夜游魂衣;
            return;
        }
        if (Phase is ReaperBurstPhase.第一附体 or ReaperBurstPhase.第二附体)
        {
            if (s.Enshrouded <= 0) return;
            BuildShroud(s);
            if (Phase == ReaperBurstPhase.第一附体)
            {
                var circleAt = float.PositiveInfinity;
                if (!_firstDot && s.CircleLeft <= 0 && s.CircleCd < 60 && s.Now >= _shroudSyncUntil)
                {
                    var firstAt = s.Lemure == 5 ? s.GcdLeft : s.Lemure == 4
                        && Math.Abs(s.GcdLeft + s.GcdElapsed - s.ReapGcd) < .1f ? -s.GcdElapsed : float.NaN;
                    _skipFirstDot = ReaperResources.CanSkipFirstDesign(s, firstAt, out circleAt, earliestCircleAt: s.FastCircle ? 0 : null);
                }
                if (!_firstDot && !_skipFirstDot && s.Lemure == 4 && s.Melee)
                {
                    if (!s.DotQt) { Replan("烙印QT关闭，按实况开环"); return; }
                    GcdAction = ReaperSkill.死亡之影;
                }
                var weave = _firstDot ? s.FastCircle || s.GcdElapsed >= s.Gcd / 2 || s.LastGcd != ReaperSkill.死亡之影
                    : _skipFirstDot && (s.FastCircle ? circleAt <= 0 && s.GcdLeft > .65f
                        : s.Lemure == 4 && s.GcdElapsed >= .65f && s.GcdLeft > .65f);
                if (s.FastCircle && _skipFirstDot && s.CircleCd <= 0 && s.CircleLeft <= 0 && !weave)
                    Reason = "快速神秘环：等待可覆盖双团契的插入位置";
                if (weave && s.CircleQt && s.CircleLeft <= 0 && s.CircleCd <= 0) OffGcdAction = ReaperSkill.神秘环;
                else if (OffGcdAction == ReaperSkill.祭性 && (s.CircleLeft <= 0 || s.Lemure > 2)) OffGcdAction = 0;
            }
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
                Cancel("没有祭品，按当前资源继续");
            return;
        }
        if (Phase == ReaperBurstPhase.收尾)
        {
            if (!_finishCombo && s.ComboNext != 0 && s.Melee) GcdAction = s.ComboNext;
            else if (s.GluttonyQt && s.GluttonyCd <= 0 && s.Soul >= 50 && !s.Locked) OffGcdAction = ReaperSkill.暴食;
            else SetPhase(ReaperBurstPhase.非爆发期);
        }
    }

    private float? EstimateCommunio(ReaperState s)
    {
        if (!s.HasTiming || s.Now < _shroudSyncUntil) return null;
        var end = s.CastingCommunio ? s.GcdLeft : s.GcdLeft + Math.Max(0, s.Lemure - 1) * s.ReapGcd + s.CommunioGcd;
        return Phase switch
        {
            ReaperBurstPhase.第一附体 when s.Enshrouded > 0 =>
                ReaperBurstRecovery.HarvestAt(s with { GcdLeft = end + (!_firstDot && !_skipFirstDot ? s.Gcd : 0) })
                + s.Gcd + 4 * s.ReapGcd + s.CommunioCast,
            ReaperBurstPhase.大丰收衔接 => (_harvestUsed ? s.GcdLeft : ReaperBurstRecovery.HarvestAt(s) + s.Gcd)
                + 4 * s.ReapGcd + s.CommunioCast,
            ReaperBurstPhase.第二附体 when s.Enshrouded > 0 =>
                s.CastingCommunio ? Math.Max(0, s.CommunioCast - s.GcdElapsed) : s.ShroudFinishIn - .65f,
            _ => null
        };
    }

    internal static uint Combo(ReaperState s) => s.ComboNext != 0 ? s.ComboNext : ReaperSkill.切割;
    internal static bool IsPositional(uint id) => id is ReaperSkill.绞决 or ReaperSkill.缢杀 or ReaperSkill.绞决处刑 or ReaperSkill.缢杀处刑;
    internal static bool IsGcd(uint id) => IsPositional(id) || id is ReaperSkill.切割 or ReaperSkill.增盈切割 or ReaperSkill.地狱切割
        or ReaperSkill.死亡之影 or ReaperSkill.灵魂切割 or ReaperSkill.勾刃 or ReaperSkill.收获月
        or ReaperSkill.虚无收割 or ReaperSkill.交错收割 or ReaperSkill.团契 or ReaperSkill.大丰收;

    private void BuildShroud(ReaperState s)
    {
        if (s.CastingCommunio) return;
        if (s.Lemure > 1) GcdAction = s.Melee ? ReaperSkill.虚无收割 : s.CanHarvestMoonForRange ? ReaperSkill.收获月 : 0;
        else if (s.Lemure == 1)
            GcdAction = !s.Moving && s.Void == 0 && s.Oblatio <= 0 && s.Distance <= GameData.GetCurrentAttackRange(25)
                ? ReaperSkill.团契 : s.CanHarvestMoonForRange ? ReaperSkill.收获月 : 0;
        if (s.Void >= 2 && s.Melee) OffGcdAction = ReaperSkill.夜游魂切割;
        else if (s.Oblatio > 0 && s.Distance <= GameData.GetCurrentAttackRange(25)) OffGcdAction = ReaperSkill.祭性;
    }

    private void BuildNormal(ReaperState s)
    {
        var quick = s.IsDump || ReaperResources.IsWindowClosing(s);
        if (s.Enshrouded > 0)
        {
            BuildShroud(s);
            if (GcdAction != ReaperSkill.收获月 && ReaperResources.NeedsDesignInShroud(s)) GcdAction = ReaperSkill.死亡之影;
            // 最后一格只有一个插入位置，先用掉虚无魂，避免团契吞掉尚未兑现的能力技。
            if (s.CircleQt && s.CircleCd <= 0 && !IsCoordinating && !(s.Lemure == 1 && (s.Void > 0 || s.Oblatio > 0)))
                OffGcdAction = ReaperSkill.神秘环;
            return;
        }
        if (s.Reavers > 0) GcdAction = s.Melee ? ReaperSkill.缢杀 : 0;
        else if (s.ComboAtRisk(s.Gcd)) GcdAction = s.ComboNext;
        else if ((!quick && ReaperResources.ShouldRefreshBeforeGluttony(s)) || ReaperResources.ShouldRefreshDeathDesign(s))
            GcdAction = ReaperSkill.死亡之影;
        else if (s.CanHarvest || ReaperBurstRecovery.HarvestNextGcd(s))
        { GcdAction = ReaperSkill.大丰收; Reason = s.EnshroudQt ? "优先大丰收，衔接免费附体" : "使用大丰收，附体QT关闭"; }
        else if (ReaperResources.AllowsHarvestMoon(s) && (!s.Melee || quick)) GcdAction = ReaperSkill.收获月;
        if (s.CircleQt && s.CircleCd <= 0 && !IsCoordinating)
        { OffGcdAction = ReaperSkill.神秘环; return; }
        if (s.Locked || GcdAction is ReaperSkill.大丰收 or ReaperSkill.死亡之影) return;
        var pendingHarvest = s.CircleQt && s.SacrificeStacks > 0 && s.SacrificeLeft > s.Bloodsown
            && s.FreeEnshroud <= 0;
        if (s.CanEnshroud && s.Melee && !IsCoordinating)
        {
            var why = "";
            var hold = !quick && !IsSimple && s.FreeEnshroud <= 0 && ReaperResources.ShouldHoldEnshroud(s, out why);
            if (why.Length > 0) Reason = why;
            // 大丰收将要解锁时，不用付费附体把整条免费链推到团辅之外。
            if (pendingHarvest && s.GcdLeft + s.SingleDuration > ReaperBurstRecovery.HarvestAt(s)) hold = true;
            var choose = !hold && (s.FreeEnshroud > 0 || !s.GluttonyQt || s.GluttonyCd > 0 || s.Soul < 50
                || s.CircleLeft > 0 && EnshroudWinsBuff(s));
            var sliceCap = !quick && s.CircleLeft <= 0 && s.SliceQt && s.SliceCharges >= 1
                && (s.FreeEnshroud <= 0 || s.FreeEnshroud > 2 * s.Gcd + s.GcdLeft + 1)
                && (2 - s.SliceCharges) * s.SliceRecast <= s.GcdLeft + s.SingleDuration + s.Gcd;
            if (choose && s.ComboAtRisk(ReaperResources.EnshroudComboDelay(s))) { GcdAction = s.ComboNext; return; }
            if (choose && s.DotQt && s.DeathDesign < s.GcdLeft + s.SingleDuration + 1 && !quick)
            { GcdAction = ReaperSkill.死亡之影; return; }
            if (choose && sliceCap && s.Soul <= 50) { GcdAction = ReaperSkill.灵魂切割; return; }
            if (choose && sliceCap && s.Soul > 50 && s.BloodQt && !ReaperResources.ShouldHoldBlood(s, out _)) choose = false;
            if (choose) { OffGcdAction = ReaperSkill.夜游魂衣; Reason = s.FreeEnshroud > 0 ? "兑现免费附体" : "使用附体"; }
        }
        if (OffGcdAction == 0 && s.Soul >= 50 && s.GluttonyQt && s.GluttonyCd <= 0
            && !s.ComboAtRisk(2 * s.Gcd)
            && (!pendingHarvest || s.GcdLeft + 2 * s.Gcd <= ReaperBurstRecovery.HarvestAt(s)))
            OffGcdAction = ReaperSkill.暴食;
        if (OffGcdAction == 0 && s.Soul >= 50 && s.BloodQt && s.Melee && !s.ComboAtRisk(s.Gcd)
            && (!pendingHarvest || s.GcdLeft + s.Gcd <= ReaperBurstRecovery.HarvestAt(s)))
        {
            if (!quick && ReaperResources.ShouldHoldBlood(s, out var why)) Reason = why;
            else OffGcdAction = ReaperSkill.隐匿挥割;
        }
        if (GcdAction == 0 && s.Melee && s.SliceQt && s.SliceCharges >= 1 && s.Soul <= 50) GcdAction = ReaperSkill.灵魂切割;
        if (GcdAction == 0 && s.IsDump) GcdAction = s.Melee ? Combo(s) : s.HarpeQt && !s.Moving ? ReaperSkill.勾刃 : 0;
    }

    private static bool EnshroudWinsBuff(ReaperState s)
    {
        float Inside(float at, int potency) => at + .15f < s.CircleLeft ? potency : 0;
        var shroud = 0f;
        for (var i = 0; i < 4; i++) shroud += Inside(s.GcdLeft + i * s.ReapGcd,
            ReaperLevelRules.Potency(s.Level, ReaperSkill.虚无收割, enhanced: i > 0));
        shroud += Inside(s.GcdLeft + 4 * s.ReapGcd + s.CommunioCast, 1100);
        shroud += Inside(s.GcdLeft + s.ReapGcd + .65f, ReaperLevelRules.Potency(s.Level, ReaperSkill.夜游魂切割));
        shroud += Inside(s.GcdLeft + 3 * s.ReapGcd + .65f, ReaperLevelRules.Potency(s.Level, ReaperSkill.夜游魂切割));
        if (ReaperLevelRules.HasSacrificium(s.Level)) shroud += Inside(.65f, 700);
        var positional = ReaperLevelRules.Potency(s.Level,
            ReaperLevelRules.HasExecutioner(s.Level) ? ReaperSkill.缢杀处刑 : ReaperSkill.缢杀, enhanced: true);
        return shroud > Inside(0, 560) + Inside(s.GcdLeft, positional) + Inside(s.GcdLeft + s.Gcd, positional);
    }

    private void ApplyReserve(ReaperState s)
    {
        if (OffGcdAction == ReaperSkill.夜游魂衣 && s.FreeEnshroud <= 0 && s.Shroud - 50 < s.GoalShroud) OffGcdAction = 0;
        if (OffGcdAction is ReaperSkill.暴食 or ReaperSkill.隐匿挥割 && s.Soul - 50 < s.GoalSoul) OffGcdAction = 0;
    }

    public bool CanWaitForHarvest => !RulesDisabled && !IsCoordinating && GcdAction == ReaperSkill.大丰收
        && Current.Alive && Current.InCombat && Current.HasTarget
        && ReaperBurstRecovery.HarvestNextGcd(IsOpener ? OpenerState(Current) : Current)
        && (IsOpener || (!Current.DotQt || Current.DeathDesign > ReaperBurstRecovery.HarvestAt(Current) + .15f)
            && (!Current.WindowActive || Current.WindowLeft > ReaperBurstRecovery.HarvestAt(Current) + .65f));

    public bool HoldHarvest(long now, string unavailable = "")
    {
        if (!CanWaitForHarvest) { StopHarvestWait("等待条件失效"); return false; }
        if (_harvestUntil > 0)
        { if (now < _harvestUntil) return true; StopHarvestWait("等待超时"); return false; }
        if (_forecastOnly || _harvestWaitUsed) return false;
        if (now < Current.Now || now - Current.Now > Current.Gcd * 1000)
        { StopHarvestWait("快照过期", true); return false; }
        _harvestWaitUsed = true;
        // 与100级采用同一经验上限；固定截止，任何重复检查都不能续期。
        _harvestUntil = now + (long)((Current.GcdLeft + ReaperBurstRecovery.HarvestMaxExtraWaitSeconds) * 1000);
        DeferHarvestQueue = true;
        return true;
    }
    public void StopHarvestWait(string reason, bool failed = false)
    { _harvestWaitUsed |= failed; _harvestUntil = 0; DeferHarvestQueue = false; }
    public bool AllowsDuringHarvestWait(uint id, long now) => !DeferHarvestQueue || !CanWaitForHarvest
        || _harvestUntil > 0 && now >= _harvestUntil
        || id is not (ReaperSkill.夜游魂衣 or ReaperSkill.暴食 or ReaperSkill.隐匿挥割 or ReaperSkill.绞决爪 or ReaperSkill.缢杀爪);

    private void UpdateQueues(ReaperState s)
    {
        if (_previous.CircleCd <= 1 && s.CircleCd > 60 && s.Bloodsown > 3) _harvestWaitUsed = false;
        if (s.TargetId != _previous.TargetId || s.EnshroudQt != _previous.EnshroudQt || s.CircleQt != _previous.CircleQt
            || s.WindowVersion != _previous.WindowVersion) StopHarvestWait("实况变化");
        if (!CanWaitForHarvest || _harvestUntil > 0 && s.Now >= _harvestUntil) StopHarvestWait("等待结束");
        DeferHarvestQueue = CanWaitForHarvest && (_harvestUntil > 0 || !_harvestWaitUsed && s.Bloodsown > 0);
        if (DeferHarvestQueue)
        {
            Reason = "短等大丰收解锁";
            if (!AllowsDuringHarvestWait(OffGcdAction, s.Now)) OffGcdAction = 0;
        }
        var chargeIn = (1 - s.SliceCharges) * s.SliceRecast;
        if (!IsPlanned && !s.IsDump && s.CircleLeft <= 0 && (!s.CircleQt || s.CircleCd > s.PreparationLead)
            && GcdAction == 0 && OffGcdAction == 0 && s.HasTiming && s.Melee && !s.Locked && s.SliceQt
            && s.Soul <= 50 && s.GcdLeft > 0 && s.GcdLeft <= .3f && chargeIn > 0 && chargeIn <= s.GcdLeft && !s.ComboAtRisk(s.Gcd))
        {
            if (_sliceUntil == 0) _sliceUntil = s.Now + (long)(s.GcdLeft * 1000);
            DeferSliceQueue = s.Now < _sliceUntil;
        }
        else _sliceUntil = 0;
    }

    public bool Allows(uint id)
    {
        if (!AllowsResource(id)) return false;
        if (IsOpener) return id == GcdAction || id == OffGcdAction;
        if (RulesDisabled) return true;
        if (id == ReaperSkill.神秘环) return !IsCoordinating && (!IsPlanned || OffGcdAction == id)
            && !(Current.Enshrouded > 0 && Current.Lemure == 1 && (Current.Void > 0 || Current.Oblatio > 0));
        if (id is ReaperSkill.夜游魂衣 or ReaperSkill.暴食) return !IsCoordinating && OffGcdAction == id;
        if (id is ReaperSkill.隐匿挥割 or ReaperSkill.绞决爪 or ReaperSkill.缢杀爪) return OffGcdAction == ReaperSkill.隐匿挥割;
        if (id == ReaperSkill.大丰收) return GcdAction == id && Current.CanHarvest;
        if (id == ReaperSkill.祭性 && Phase == ReaperBurstPhase.第一附体 && (Current.CircleLeft <= 0 || Current.Lemure > 2)) return false;
        return true;
    }
    public bool AllowsResource(uint id, bool fallback = false)
    {
        // 首帧读取失败时可能还没有等级快照；降级后交给派发层的原生等级/任务校验。
        if (!RulesDisabled && !ReaperLevelRules.Learned(Current.Level, id)) return false;
        if (!AllowsDuringHarvestWait(id, _forecastOnly ? Current.Now : Environment.TickCount64)) return false;
        if (IsOpener || !Current.WindowActive) return true;
        if (id == ReaperSkill.夜游魂衣 && (Current.WindowLeft > 0 ? Current.WindowLeft : Current.WindowLimit) <= .8f) return false;
        if (id == OffGcdAction && !fallback && !RulesDisabled) return true;
        if (id == ReaperSkill.夜游魂衣) return Current.FreeEnshroud > 0 || Current.Shroud - 50 >= Current.GoalShroud;
        if (id is ReaperSkill.暴食 or ReaperSkill.隐匿挥割 or ReaperSkill.绞决爪 or ReaperSkill.缢杀爪) return Current.Soul - 50 >= Current.GoalSoul;
        return true;
    }
    public IEnumerable<uint> DumpCandidates(ReaperState s, bool off) => ReaperLevel90Projection.Candidates(s, off);
    public bool CanDump(ReaperState s, uint id, bool off) => ReaperLevel90Projection.CanUse(s, id, off);
}
