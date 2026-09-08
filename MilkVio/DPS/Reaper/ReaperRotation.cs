using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using ECommons.ExcelServices;
using ECommons.Logging;
using ECommons.DalamudServices;
using PromeRotation.Data;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using MilkVio.Common;
using MilkVio.DPS.Reaper.Action.Gcd;
using MilkVio.DPS.Reaper.Action.OffGcd;
using MilkVio.DPS.Reaper.ReaperData;
using PromeRotation.Windows;
using PromeRotation.Helpers;
using PromeRotation.Extensions;
using PromeRotation.Hosting;
using MilkVio.DPS.Reaper.Opener;
using System.Text;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Reaper;

[RotationMetadata((uint)Job.RPR, "自用钐镰客", "MilkVio", GlobalVersion.Version, ContentScope = AcrContentScope.HighEnd)]
public class ReaperRotation : IRotation, IRotationLifecycle
{
    private readonly IRotationEventHandler _eventHandler = new ReaperRotationEventHandler();
    public IRotationEventHandler GetEventHandler() => _eventHandler;

    private readonly List<IDecisionResolver> _alwaysResolvers = new();
    private readonly List<IDecisionResolver> _gcdResolvers = new();
    private readonly List<IDecisionResolver> _offGcdResolvers = new();
    private readonly OpenerSelector _openerSelector = new();
    private long _lastResolverErrorAt;
    private readonly List<IDisposable> _subscriptions = new();
    private string _copyResult = "";
    private long _copyResultUntil;
    private bool _skipPlannerOnce;
    private float _simulationSeconds = 120;
    private bool _simulationKeepResources;
    private int _simulationSoul = 50, _simulationShroud = 50;
    private int _simulationWindowVersion = -1;
    private string _simulationError = "";
    private static ReaperBurstPlanner Planner => ReaperBattleData.Instance.Planner;
    public static IJobNodeProvider NodeProvider { get; } = new ReaperJobNodeProvider();

    public static IReadOnlyDictionary<string, bool> QtList { get; } = new Dictionary<string, bool>
    {
        {ReaperQt.神秘环, true},
        {ReaperQt.附体, true},
        {ReaperQt.灵魂割, true},
        
        {ReaperQt.隐匿挥割, true},
        {ReaperQt.暴食, true},
        {ReaperQt.远离完人, false},
        
        {ReaperQt.Dot, true},
        {ReaperQt.收获月, true},
        {ReaperQt.倾泻资源, false},
        
        {ReaperQt.AOE, false},
        {ReaperQt.勾刃, true},
        {ReaperQt.真北, true},
    };
    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>
    {
        { "镰刀100级标准起手", typeof(RPR_100_Standard) },
        { "镰刀100级妖星起手", typeof(RPR_100_DMU) }
    };

    public ReaperRotation()
    {
        // 在这里，按照优先级从高到低的顺序，注册所有的解析器
        // 爆发相关的oGCD优先级最高
        _offGcdResolvers.Add(new 神秘环OffGcd());
        _offGcdResolvers.Add(new 附体OffGcd());
        _offGcdResolvers.Add(new 附体连击OffGcd());
        _offGcdResolvers.Add(new 祭牲OffGcd());
        _offGcdResolvers.Add(new 暴食OffGcd());
        _offGcdResolvers.Add(new 隐匿挥割OffGcd());
        _offGcdResolvers.Add(new 真北OffGcd());

        // 爆发状态下的GCD
        _gcdResolvers.Add(new 附体连击Gcd());
        _gcdResolvers.Add(new 绞决缢杀Gcd());
        _gcdResolvers.Add(new 连击保护Gcd());
        _gcdResolvers.Add(new 大丰收());
        _gcdResolvers.Add(new 完人Gcd());
        _gcdResolvers.Add(new DotGcd());
        _gcdResolvers.Add(new 收获月Gcd());
        _gcdResolvers.Add(new 灵魂切割Gcd());
        _gcdResolvers.Add(new 基础连击Gcd());
        _gcdResolvers.Add(new 勾刃Gcd());

        // 画QT
        foreach (var (name, def) in QtList)
            PromeSettings.Instance.AddQt(name, def);
    }

    public IOpener? GetOpener()
    {
        if (!ReaperSettings.Instance.启用起手 || Core.Me == null || Core.Me.Level < 100)
            return null;

        var opener = _openerSelector.Resolve(Openers);
        ReaperBattleData.Instance.OpenerName = opener?.GetType().Name ?? "无起手";
        if (opener is not RPR_100_DMU) ReaperBattleData.Instance.DmuOpener.Reset();
        return opener;
    }

    public PAction? NextAlways() => RecordSelection(ResolveAlways());
    public PAction? NextGcd() => RecordSelection(ResolveGcd());
    public PAction? NextOffGcd() => RecordSelection(ResolveOffGcd());

    private static PAction? RecordSelection(PAction? action)
    {
        if (action != null) RecordRequest(action, $"{Planner.ModeName}/{Planner.Reason} 建议={Planner.GcdAction}/{Planner.OffGcdAction}");
        return action;
    }

    internal static void RecordRequest(PAction action, string reason)
    {
        var log = ReaperBattleData.Instance.DebugLog;
        if (!log.Enabled) return;
        try { log.Request(action.ActionId, action.Type == ActionType.OffGcd, ReadState(), reason); }
        catch (Exception ex) { log.Fail(ex); }
    }

    private PAction? ResolveAlways()
    {
        if (ReaperBattleData.Instance.DmuOpener.Active) return null;
        foreach (var resolver in _alwaysResolvers)
        {
            var action = TryResolve(resolver, out _);
            if (action != null) return action;
        }
        return null;
    }

    private PAction? ResolveGcd()
    {
        if (ReaperBattleData.Instance.DmuOpener.Active) return null;
        UpdatePlanner(true);
        _skipPlannerOnce = false;
        if (!Planner.RulesDisabled && !Planner.Current.HasTarget) return null;
        if (TryHarvestWait(out var harvest)) return harvest;
        var planned = TryPlannedAction(Planner.GcdAction, ActionType.Gcd);
        if (planned != null) return planned;
        if (DumpEnabled) return _skipPlannerOnce || Planner.RulesDisabled ? TryDumpFallback(ActionType.Gcd) : null;
        if (!Planner.RulesDisabled && Planner.DeferSliceQueue) return null;
        foreach (var resolver in _gcdResolvers)
        {
            var action = TryResolve(resolver, out _, _skipPlannerOnce);
            if (action != null) return action;
        }
        return null;
    }

    private bool TryHarvestWait(out PAction? action)
    {
        action = null;
        try
        {
            if (!Planner.CanWaitForHarvest) { Planner.StopHarvestWait("等待条件失效"); return false; }
            var fresh = ReadState();
            if (!ReaperProjection.SameDecision(fresh, Planner.Current)) Planner.Update(fresh, true);
            if (!Planner.CanWaitForHarvest) { Planner.StopHarvestWait("等待条件失效"); return false; }

            // 只尝试已选中的大丰收；短暂不可用不取消爆发，也不落到123。
            var resolver = new 大丰收();
            var harvest = resolver.GetAction();
            var check = resolver.Check();
            uint? nativeStatus = null;
            if (check.Success && ReaperHelper.QtAllows(harvest.ActionId) && ReaperHelper.当前可执行(harvest, out nativeStatus))
            { action = harvest; return true; }
            return Planner.HoldHarvest(Environment.TickCount64,
                check.Success ? $"原生状态={nativeStatus?.ToString() ?? "未检查"}" : check.Message);
        }
        catch (Exception)
        {
            Planner.StopHarvestWait("等待检查异常，恢复普通求解", failed: true);
            _skipPlannerOnce = true;
            return false;
        }
    }

    private PAction? ResolveOffGcd()
    {
        if (ReaperBattleData.Instance.DmuOpener.Active) return null;
        UpdatePlanner(true);
        _skipPlannerOnce = false;
        if (!Planner.RulesDisabled && !Planner.Current.HasTarget) return null;
        var planned = TryPlannedAction(Planner.OffGcdAction, ActionType.OffGcd);
        if (planned != null) return planned;
        if (DumpEnabled)
            return (_skipPlannerOnce || Planner.RulesDisabled ? TryDumpFallback(ActionType.OffGcd) : null)
                ?? TryResolve(new 真北OffGcd(), out _);
        foreach (var resolver in _offGcdResolvers)
        {
            var action = TryResolve(resolver, out _, _skipPlannerOnce);
            if (action != null) return action;
        }
        return null;
    }

    // 单个求解器失效就检查下一项，普通连击/勾刃仍在列表末尾兜底。
    private PAction? TryResolve(IDecisionResolver resolver, out CheckResult result, bool skipPlanner = false)
    {
        result = new CheckResult(false, "自身未加载或已死亡");
        if (Core.Me == null || Core.Me.IsDead) return null;
        try
        {
            result = resolver.Check();
            if (!result.Success) return null;
            var action = resolver.GetAction();
            if (action != null && (skipPlanner || Planner.Allows(action.ActionId)) && Planner.AllowsResource(action.ActionId, skipPlanner)
                && ReaperHelper.QtAllows(action.ActionId)
                && ReaperHelper.当前可执行(action)
                && (resolver is not 真北OffGcd || resolver.Check().Success)) return action;
            result = new CheckResult(false, "当前技能不可用 继续检查后续技能");
        }
        catch (Exception ex)
        {
            result = new CheckResult(false, "求解异常 已跳过");
            var now = Environment.TickCount64;
            if (now - _lastResolverErrorAt >= 5000)
            {
                _lastResolverErrorAt = now;
                PluginLog.Warning($"[Reaper] {resolver.GetType().Name}: {ex.Message}");
            }
        }
        return null;
    }

    public static void UpdatePlanner(bool evaluateResources = false)
    {
        try { Planner.Update(ReadState(), evaluateResources); }
        catch (Exception) { Planner.DisableRules(); }
    }

    internal static void UpdateDebugLog()
    {
        var data = ReaperBattleData.Instance;
        if (!data.DebugLog.Enabled) return;
        try
        {
            data.DebugLog.Frame(ReadState(), Planner,
                string.Join(" ", QtList.Keys.Select(qt => $"{qt}={PromeSettings.Instance.GetQt(qt)}")),
                data.Window.Version, data.Window.DebugDetails(), Core.Me is { IsCasting: true } me ? me.CastActionId : 0);
        }
        catch (Exception ex) { data.DebugLog.Fail(ex); }
    }

    private static bool DumpEnabled => !ReaperBattleData.Instance.Window.Active && PromeSettings.Instance.GetQt(ReaperQt.倾泻资源);

    private PAction? TryDumpFallback(ActionType type)
    {
        try
        {
            var state = ReadState();
            if (!state.IsDump || !state.Alive || !state.HasTarget) return null;
            foreach (var id in ReaperDumpPlanner.Candidates(state, type == ActionType.OffGcd).Distinct())
            {
                if (id == 0 || !ReaperDumpPlanner.CanUse(state, id, type == ActionType.OffGcd)) continue;
                var action = CreateAction(id, type);
                if (Planner.AllowsDuringHarvestWait(action.ActionId, Environment.TickCount64)
                    && ReaperHelper.QtAllows(action.ActionId) && ReaperHelper.当前可执行(action))
                {
                    Planner.NoteDumpFallback(id, type == ActionType.OffGcd);
                    return action;
                }
            }
        }
        catch (Exception) { }
        return null;
    }

    private static PAction CreateAction(uint id, ActionType type)
    {
        if (id == ReaperSkill.虚无收割 && ActionHelper.IsActionHighlighted(ReaperSkill.交错收割)) id = ReaperSkill.交错收割;
        return id == ReaperSkill.缢杀 ? new 绞决缢杀Gcd().GetAction()
            : new PAction(id == ReaperSkill.隐匿挥割 ? id.GetAdjustedActionId() : id, type,
                id is ReaperSkill.神秘环 or ReaperSkill.夜游魂衣 ? ActionTargetType.Self : ActionTargetType.Target);
    }

    public static ReaperState ReadState()
    {
        var state = ReaperState.Read();
        bool? noEnemies = null;
        uint? weather = null;
        if (ReaperBattleData.Instance.Window.Active)
        {
            var detection = ReaperBattleData.Instance.Window.DetectionNeeded(state.Now);
            try { if (detection.Enemies && state.Alive && state.PlayerId != 0) noEnemies = TargetHelper.IsAllBossUntargetable(); } catch (Exception) { }
            try { if (detection.Weather) weather = GameData.Weather; } catch (Exception) { }
        }
        return ReaperBattleData.Instance.Window.Update(state, noEnemies, weather);
    }

    private PAction? TryPlannedAction(uint id, ActionType type)
    {
        if (id == 0) return null;
        try
        {
            var fresh = ReadState();
            if (!ReaperProjection.SameDecision(fresh, Planner.Current))
            {
                Planner.Update(fresh, true);
                id = type == ActionType.Gcd ? Planner.GcdAction : Planner.OffGcdAction;
                if (id == 0) return null;
            }
            var action = CreateAction(id, type);
            if (!Planner.AllowsDuringHarvestWait(action.ActionId, Environment.TickCount64)) return null;
            if (ReaperHelper.QtAllows(action.ActionId) && ReaperHelper.当前可执行(action)) return action;
        }
        catch (Exception) { }
        _skipPlannerOnce = true;
        if (Planner.IsPlanned)
        {
            Planner.Cancel($"计划技能{id}当前不可用");
            UpdatePlanner();
        }
        return null;
    }

    public void OnEnterAcr()
    {
        OnExitAcr();
        ReaperBattleData.Instance.RenewDebugLog();
        _subscriptions.Add(PromeEventBus.OnCountdownStarted(this, () => ReaperBattleData.Instance.AutoSoulsow.Begin()));
        _subscriptions.Add(PromeEventBus.OnActionEffect(this, e =>
        {
            ReaperBattleData.Instance.DebugLog.ObserveEffect(e.SourceId, e.ActionId, e.GlobalSequence, Environment.TickCount64);
            Planner.EnqueueAction(e.SourceId, e.ActionId, e.GlobalSequence);
            ReaperBattleData.Instance.DmuOpener.EnqueueAction(e.SourceId, e.ActionId, Environment.TickCount64);
            ReaperBattleData.Instance.AutoSoulsow.ObserveAction(e.SourceId, e.ActionId);
            ReaperBattleData.Instance.Window.Observe(ReaperWindowEvent.技能效果, e.ActionId, Environment.TickCount64, e.Timestamp);
        }));
        _subscriptions.Add(PromeEventBus.OnStartCast(this, e =>
        {
            ReaperBattleData.Instance.DebugLog.ObserveCast(e.SourceId, e.ActionId, Environment.TickCount64);
            ReaperBattleData.Instance.Window.Observe(ReaperWindowEvent.开始咏唱, e.ActionId, Environment.TickCount64, e.Timestamp);
        }));
        _subscriptions.Add(PromeEventBus.OnPlayerDied(this, () =>
        {
            Planner.Reset("死亡，清除旧计划");
            ReaperBattleData.Instance.AutoSoulsow.Cancel();
            ReaperBattleData.Instance.DmuOpener.Reset("死亡，结束妖星起手");
        }));
        _subscriptions.Add(PromeEventBus.OnPlayerRevived(this, () => Planner.Reset("复活，按当前红绿重建")));
    }

    public void OnExitAcr()
    {
        foreach (var subscription in _subscriptions) subscription.Dispose();
        _subscriptions.Clear();
        ReaperBattleData.Instance.DebugLog.Shutdown(Environment.TickCount64);
        ReaperBattleData.Instance.Reset("切换/卸载ACR");
        _simulationError = _copyResult = "";
        _copyResultUntil = 0;
        _skipPlannerOnce = false;
    }

    public void UpdateDebugStatus()
    {
        RotationManager.AlwaysSolverStatus.Clear();
        RotationManager.GcdSolverStatus.Clear();
        RotationManager.OffGcdSolverStatus.Clear();

        foreach (var resolver in _alwaysResolvers)
        {
            TryResolve(resolver, out var result);
            RotationManager.AlwaysSolverStatus.Add(new SolverStatus
            {
                Name = resolver.GetType().Name,
                Success = result.Success,
                Message = result.Message
            });
        }

        foreach (var resolver in _gcdResolvers)
        {
            TryResolve(resolver, out var result);
            RotationManager.GcdSolverStatus.Add(new SolverStatus
            {
                Name = resolver.GetType().Name,
                Success = result.Success,
                Message = result.Message
            });
        }

        foreach (var resolver in _offGcdResolvers)
        {
            TryResolve(resolver, out var result);
            RotationManager.OffGcdSolverStatus.Add(new SolverStatus
            {
                Name = resolver.GetType().Name,
                Success = result.Success,
                Message = result.Message
            });
        }
    }

    public void DrawQTs()
    {

    }

    public void DrawSettings()
    {
        if (ImGui.BeginTabBar("Settings"))
        {
            if (ImGui.BeginTabItem("设置"))
            {
                DrawGeneral();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("开发用"))
            {
                DrawDev();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawGeneral()
    {
        _openerSelector.DrawCombo("起手选择", Openers);
        ImGui.Checkbox(ReaperQt.启用起手, ref ReaperSettings.Instance.启用起手);
        var harpeAt = ReaperSettings.Instance.勾刃倒数预读时间;
        if (ImGui.InputFloat("勾刃倒数预读（秒，下次倒数生效）", ref harpeAt))
            ReaperSettings.Instance.勾刃倒数预读时间 = harpeAt;
        if (ImGui.Checkbox("倒计时自动播魂", ref ReaperSettings.Instance.倒计时自动播魂)
            && !ReaperSettings.Instance.倒计时自动播魂) ReaperBattleData.Instance.AutoSoulsow.Cancel();
    }

    private void DrawDev()
    {
        if (!ImGui.BeginTabBar("ReaperDev")) return;
        if (ImGui.BeginTabItem("基础信息"))
        {
            DrawBasicInfo();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("模拟输出窗口"))
        {
            DrawWindowSimulation();
            ImGui.EndTabItem();
        }
        ImGui.EndTabBar();
    }

    private void DrawBasicInfo()
    {
        var normal = ReaperHelper.普通Gcd;
        var enshroud = ReaperHelper.附体Gcd;
        ImGui.TextUnformatted(normal.HasValue ? $"普通 GCD：{normal.Value:F2} 秒" : "普通 GCD：未读取");
        ImGui.TextUnformatted(enshroud.HasValue ? $"附体 GCD（收割）：{enshroud.Value:F2} 秒" : "附体 GCD（收割）：未读取");
        var state = Planner.Current;
        var circle = state.CircleLeft > 0 ? $"剩余 {state.CircleLeft:F1} 秒" : $"冷却 {state.CircleCd:F1} 秒";
        var phase = Planner.ModeName;
        var finish = Planner.BurstCommunioIn.HasValue ? $"｜预计团契 {Planner.BurstCommunioIn.Value:F1} 秒后" : "";
        if (Planner.BurstFinishIn.HasValue) finish += $"｜完人 {Planner.BurstFinishIn.Value:F1} 秒后";
        ImGui.TextUnformatted($"阶段：{phase}｜神秘环{circle}{finish}");
        ImGui.TextUnformatted($"决策：{Planner.Reason}｜GCD {PlannedActionName(Planner.GcdAction)} / 能力技 {PlannedActionName(Planner.OffGcdAction)}");
        ImGui.TextUnformatted($"快速神秘环：{(ReaperBattleData.Instance.FastCircle ? "开启" : "关闭")}");
        var green = Planner.ForecastGreenText;
        ImGui.TextUnformatted($"资源：红 {state.Soul} / 绿 {state.Shroud}｜用本次附体后120预计绿 {green}");
        if (ReaperBattleData.Instance.DmuOpener.Active)
            ImGui.TextUnformatted(ReaperBattleData.Instance.DmuOpener.Status);
        if (ReaperBattleData.Instance.Window.Active)
            ImGui.TextUnformatted(ReaperBattleData.Instance.Window.Describe(Environment.TickCount64));
        DrawCopyDebug();
        ImGui.SameLine();
        var data = ReaperBattleData.Instance;
        if (ImGui.Button(data.DebugLog.Enabled ? "停止记录Debug" : "开始记录Debug"))
        {
            if (data.DebugLog.Enabled) data.DebugLog.Disable(Environment.TickCount64);
            else
            {
                if (data.DebugLog.Error.Length > 0) data.RenewDebugLog();
                try
                {
                    data.DebugLog.Enable(ReadState(), $"Reaper {GlobalVersion.Version} 地图={Svc.ClientState.TerritoryType} 起手={data.OpenerName}");
                    UpdateDebugLog();
                }
                catch (Exception ex) { data.DebugLog.Fail(ex); }
            }
        }
        if (data.DebugLog.Error.Length > 0) ImGui.TextUnformatted(data.DebugLog.Error);
        else if (data.DebugLog.Enabled) ImGui.TextUnformatted(data.DebugLog.FilePath.Length > 0 ? "Debug记录中（每场战斗一份）" : "Debug已开启，等待开战");
    }

    private void DrawWindowSimulation()
    {
        var window = ReaperBattleData.Instance.Window;
        if (_simulationWindowVersion != window.Version)
        {
            _simulationWindowVersion = window.Version;
            _simulationError = "";
        }
        ImGui.SetNextItemWidth(120);
        ImGui.InputFloat("爆发区间时长（秒）", ref _simulationSeconds);
        ImGui.Checkbox("保留期末资源", ref _simulationKeepResources);
        ImGui.SameLine();
        ImGui.BeginDisabled(!_simulationKeepResources);
        ImGui.SetNextItemWidth(90);
        if (ImGui.InputInt("红", ref _simulationSoul)) _simulationSoul = Math.Clamp(_simulationSoul, 0, 100);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(90);
        if (ImGui.InputInt("绿", ref _simulationShroud)) _simulationShroud = Math.Clamp(_simulationShroud, 0, 100);
        ImGui.EndDisabled();

        if (ImGui.Button("开始模拟"))
        {
            var settings = new ReaperWindowSettings
            {
                Seconds = _simulationSeconds, KeepResources = _simulationKeepResources,
                Soul = _simulationSoul, Shroud = _simulationShroud
            };
            if (window.Set(settings, null, Environment.TickCount64, out _simulationError))
            {
                Planner.WindowChanged("手动开始输出窗口模拟");
                UpdatePlanner();
            }
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(!window.IsSimulation);
        if (ImGui.Button("停止模拟") && window.IsSimulation)
        {
            window.Clear("手动模拟已停止");
            Planner.WindowChanged("手动停止输出窗口模拟");
            UpdatePlanner();
        }
        ImGui.EndDisabled();
        if (_simulationError.Length > 0) ImGui.TextUnformatted(_simulationError);

        ImGui.Separator();
        ImGui.TextUnformatted(window.Active ? window.Describe(Environment.TickCount64) : $"当前无输出窗口｜{window.LastReason}");
        if (window.Active)
        {
            var state = Planner.Current;
            var phase = Planner.ModeName;
            ImGui.TextUnformatted($"当前红 {state.Soul} / 绿 {state.Shroud}｜阶段：{phase}");
            ImGui.TextUnformatted($"决策：{Planner.Reason}｜GCD {PlannedActionName(Planner.GcdAction)} / 能力技 {PlannedActionName(Planner.OffGcdAction)}");
        }
        DrawCopyDebug();
    }

    private void DrawCopyDebug()
    {
        if (ImGui.Button("复制 Debug 信息"))
        {
            try
            {
                var state = ReadState();
                var text = new StringBuilder();
                text.AppendLine($"Reaper {GlobalVersion.Version} / {DateTime.Now:O}");
                text.AppendLine($"阶段={Planner.ModeName} 原因={Planner.Reason} 兜底={Planner.IsSimple} QT协调={Planner.IsCoordinating}");
                text.AppendLine($"建议GCD={Planner.GcdAction} 建议能力技={Planner.OffGcdAction} 花本次附体后预计120魂衣={Planner.ForecastGreenText}");
                text.AppendLine($"短等大丰收={Planner.DeferHarvestQueue}");
                text.AppendLine($"快速神秘环={ReaperBattleData.Instance.FastCircle} 倒数自动播魂={ReaperSettings.Instance.倒计时自动播魂} 待播魂={ReaperBattleData.Instance.AutoSoulsow.Active} 勾刃预读设置={ReaperSettings.Instance.勾刃倒数预读时间:F3}");
                text.AppendLine(ReaperBattleData.Instance.DmuOpener.Status);
                text.AppendLine($"预计团契秒数={Planner.BurstCommunioIn} 预计完人秒数={Planner.BurstFinishIn} 最近降级原因={Planner.FallbackReason}");
                text.AppendLine(state.ToString());
                text.AppendLine(ReaperBattleData.Instance.Window.Describe(state.Now));
                text.AppendLine(ReaperBattleData.Instance.Window.DebugDetails());
                text.AppendLine(Planner.ProjectionDebug);
                if (!state.IsDump)
                {
                    text.AppendLine($"暴食预计可用：保留={ReaperResources.EstimateGluttony(state, false)} 消费={ReaperResources.EstimateGluttony(state, true)}");
                    text.AppendLine($"本次附体后暴食={ReaperResources.EstimateGluttony(state, false, enshroudNow: true)} 灵魂切割暂缓预排={Planner.DeferSliceQueue}");
                }
                foreach (var qt in QtList.Keys) text.AppendLine($"QT {qt}={PromeSettings.Instance.GetQt(qt)}");
                foreach (var recent in Planner.RecentActions) text.AppendLine(recent);
                ImGui.SetClipboardText(text.ToString());
                _copyResult = "已复制";
            }
            catch (Exception) { _copyResult = "复制失败"; }
            _copyResultUntil = Environment.TickCount64 + 3000;
        }
        if (Environment.TickCount64 < _copyResultUntil) { ImGui.SameLine(); ImGui.TextUnformatted(_copyResult); }
    }

    private static string PlannedActionName(uint id)
    {
        if (id == 0) return "—";
        if (id == ReaperSkill.缢杀) return "身位技";
        if (id == ReaperSkill.虚无收割) return "交替收割";
        try { return Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id).GetActionName(); }
        catch (Exception) { return id.ToString(); }
    }

}
