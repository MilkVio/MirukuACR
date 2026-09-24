using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using MilkVio.Common;
using MilkVio.DPS.Machinist.Action.Gcd;
using MilkVio.DPS.Machinist.Action.OffGcd;
using MilkVio.DPS.Machinist.MCHData;
using MilkVio.DPS.Machinist.Opener;
using MilkVio.DPS.UniversalData;
using PromeRotation.Timeline;
using PromeRotation.UI;
using PromeRotation.UI.HotKey;
using PromeRotation.Updaters;
using PromeRotation.Windows;
using PromeRotation.Helpers;
using PromeRotation.Hosting;
using PromeRotation.Timeline.Core;
using MilkVio.DPS.Machinist.Planning;

namespace MilkVio.DPS.Machinist;

[RotationMetadata((uint)Job.MCH, "玉玉机工", "MilkVio", GlobalVersion.Version, ContentScope = AcrContentScope.HighEnd)]
public class MachinistRotation : IRotation, IRotationLifecycle
{
    // 创建一个属于该职业的回调
    private readonly IRotationEventHandler _eventHandler;
    public IRotationEventHandler GetEventHandler() => _eventHandler;
    public static IJobNodeProvider NodeProvider { get; } = new MachinistJobNodeProvider();
    private readonly List<IDisposable> _subscriptions = new();
    internal MachinistDebugLog DebugLog { get; private set; } = CreateDebugLog();
    private static MachinistDebugLog CreateDebugLog() => new(() => System.IO.Path.Combine(
        Svc.PluginInterface.ConfigDirectory.FullName, "ACR", "MilkVio", "DebugLog"));
    private long _lastPlanningError;
    
    // 管理该职业所有的决策解析器
    private readonly List<IDecisionResolver> _alwaysResolvers = new();
    private readonly List<IDecisionResolver> _gcdResolvers = new();
    private readonly List<IDecisionResolver> _offGcdResolvers = new();
    private readonly OpenerSelector _openerSelector = new();
    
    // 实现对外暴露的静态属性
    // Qt列表
    public static IReadOnlyDictionary<string, bool> QtList { get; } = new Dictionary<string, bool>
    {
        {MCHQt.枪管加热, true},
        {MCHQt.野火, true},
        {MCHQt.超荷, true},
        {MCHQt.机器人, true},
        {MCHQt.整备, true},
        {MCHQt.AOE, true},
        {MCHQt.先打飞锯, false},
        {MCHQt.倾泻资源, false},
        {MCHQt.快速爆发, false},
    };
    // 起手列表
    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>
    {
        {"机工80绝亚DollSkip起手", typeof(MCH_80_TEADS)},
        {"机工90绝欧起手", typeof(MCH_90_OMG)},
        {"机工100通用起手", typeof(MCH_100_G)},
        {"机工100通用钻头起手", typeof(MCH_100_Drill)},
        {"机工妖星起手", typeof(MCH_100_DMU)},
    };
    
    public MachinistRotation()
    {
        _eventHandler = new MachinistRotationEventHandler(this);
        // 在这里，按照优先级从高到低的顺序，注册所有的解析器
        // 爆发相关的oGCD优先级最高
        _offGcdResolvers.Add(new 野火OffGcd());
        _offGcdResolvers.Add(new 枪管加热OffGcd());
        _offGcdResolvers.Add(new 机器人OffGcd());
        _offGcdResolvers.Add(new 超荷OffGcd());
        _offGcdResolvers.Add(new 虹吸弹OffGcd());
        _offGcdResolvers.Add(new 弹射OffGcd());
        
        // 爆发状态下的GCD
        _gcdResolvers.Add(new 空气锚Gcd());
        _gcdResolvers.Add(new 钻头Gcd());
        _gcdResolvers.Add(new 回转飞锯Gcd());
        _gcdResolvers.Add(new 掘地飞轮Gcd());
        _gcdResolvers.Add(new 全金属爆发Gcd());
        _gcdResolvers.Add(new 过热Gcd());
        _gcdResolvers.Add(new 基础Gcd());
        
        // 画QT
        foreach (var (name, def) in QtList)
            PromeSettings.Instance.AddQt(name, def);
        
        var hotkeyPanel = new HotkeyPanel(columns: 5, title: "MCH Hotkeys");
        hotkeyPanel.AddHotkey("亲疏自行", new PAction(MeleeUniversalSkill.亲疏自行, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("内丹", new PAction(MeleeUniversalSkill.内丹, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey(
            "同步镜头",
            new ToggleLogic(
                () => CameraSyncManager.CurrentMode == SyncMode.Camera,
                CameraSyncManager.ToggleCameraSync),
            iconActionId: 11404);
        hotkeyPanel.AddHotkey(
            "校准正方向",
            new ToggleLogic(
                () => CameraSyncManager.CurrentMode == SyncMode.Align,
                CameraSyncManager.ToggleAlignSync),
            customIconPath: "Resources/Align4.png");
        hotkeyPanel.AddHotkey(
            "清扫队列",
            new ExecuteLogic(() =>
            {
                ActionQueueManager.ClearAllQueues();
                ActionUpdater.Reset();
                Svc.Chat.PrintError("[PromeRotation] 清扫队列");
            }),
            customIconPath: "Resources/Clear.png");
        HotkeyManager.Instance.AddHotkeyPanel(hotkeyPanel);
    }
    
    public void OnEnterAcr()
    {
        OnExitAcr();
        DebugLog = CreateDebugLog();
        MachinistPlanning.WriteNote = note => DebugLog.ObserveNote(Environment.TickCount64, note);
        MachinistPlanning.Enter();
        MachinistHelper.EnterWeaveLimit();
        _subscriptions.Add(PromeEventBus.OnActionEffect(this, e =>
        {
            var now = Environment.TickCount64;
            MachinistPlanning.ObserveEffect(e.SourceId, e.TargetId, e.ActionId, e.GlobalSequence, now);
            DebugLog.ObserveEffect(e.SourceId, e.TargetId, e.ActionId, e.GlobalSequence, now);
            MachinistPlanning.Window.Observe(MachinistWindowEvent.技能效果, e.ActionId, now, e.Timestamp);
        }));
        _subscriptions.Add(PromeEventBus.OnStartCast(this, e =>
        {
            DebugLog.ObserveCast(e.SourceId, e.TargetId, e.ActionId, Environment.TickCount64);
            MachinistPlanning.Window.Observe(MachinistWindowEvent.开始咏唱, e.ActionId, Environment.TickCount64, e.Timestamp);
        }));
        _subscriptions.Add(PromeEventBus.OnPlayerDied(this, () => MachinistPlanning.Reset("死亡")));
        _subscriptions.Add(PromeEventBus.OnPlayerRevived(this, () => MachinistPlanning.Reset("复活")));
    }

    public void OnExitAcr()
    {
        foreach (var subscription in _subscriptions) subscription.Dispose();
        _subscriptions.Clear();
        MachinistPlanning.Exit();
        MachinistHelper.ExitWeaveLimit();
        DebugLog.Shutdown(Environment.TickCount64);
    }

    // 该职业的起手
    public IOpener? GetOpener()
    {
        if (!MCHSettings.Instance.启用起手 || Core.Me == null)
            return null;

        return _openerSelector.Resolve(Openers);
    }
    
    public PAction? NextAlways()
    {
        foreach (var resolver in _alwaysResolvers)
        {
            if (resolver.Check().Success)
                return resolver.GetAction();
        }
        return null;
    }

    public PAction? NextGcd()
    {
        if (Core.Me == null || Core.Me.IsDead) return null;
        if (Core.Me.Level == 100)
        {
            try
            {
                var action = MachinistPlanning.NextGcd(out var reason);
                Record(action, false, reason);
                return action;
            }
            catch (Exception ex) { PlanningError(ex); }
        }
        var fallback = ResolveLegacyGcd();
        Record(fallback, false, "基础循环");
        return fallback;
    }

    private PAction? ResolveLegacyGcd()
    {
        var held = MachinistPlanning.HeldTool;
        // 遍历所有GCD解析器
        foreach (var resolver in _gcdResolvers)
        {
            if (resolver.Check().Success)
            {
                // 找到第一个满足条件的，返回它的决策结果
                var action = resolver.GetAction();
                if (action?.ActionId != held) return action;
            }
        }
        // 如果所有求解器都不满足条件，返回null
        return null;
    }
    
    public PAction? NextOffGcd()
    {
        if (Core.Me == null || Core.Me.IsDead) return null;
        try
        {
            var planned = MachinistPlanning.NextOffGcd(ResolveLegacyGcd, out var reason);
            if (planned != null || Core.Me.Level == 100)
            {
                Record(planned, true, reason);
                return planned;
            }
        }
        catch (Exception ex)
        {
            PlanningError(ex);
            if (Core.Me.Level == 100) return null;
        }
        // 遍历所有oGCD解析器 同上
        foreach (var resolver in _offGcdResolvers)
        {
            if (resolver.Check().Success)
            {
                return MachinistPlanning.PrepareLegacyOffGcd(resolver.GetAction());
            }
        }
        return null;
    }
    
    public void UpdateDebugStatus()
    {
        // 清空上一帧的旧数据
        RotationManager.AlwaysSolverStatus.Clear();
        RotationManager.GcdSolverStatus.Clear();
        RotationManager.OffGcdSolverStatus.Clear();

        if (Core.Me?.Level == 100)
        {
            RotationManager.GcdSolverStatus.Add(new SolverStatus { Name = "MCH100共用规划", Success = true, Message = MachinistPlanning.Description });
            RotationManager.OffGcdSolverStatus.Add(new SolverStatus { Name = "MCH100资源与野火", Success = true, Message = MachinistPlanning.Description });
            return;
        }

        foreach (var resolver in _alwaysResolvers)
        {
            var result = resolver.Check();

            RotationManager.AlwaysSolverStatus.Add(new SolverStatus
            {
                Name = resolver.GetType().Name,
                Success = result.Success,
                Message = result.Message
            });
        }
        
        // GCD状态列表
        foreach (var resolver in _gcdResolvers)
        {
            var result = resolver.Check();
            
            RotationManager.GcdSolverStatus.Add(new SolverStatus
            {
                Name = resolver.GetType().Name,
                Success = result.Success,
                Message = result.Message
            });
        }
        
        // OGCD状态列表
        foreach (var resolver in _offGcdResolvers)
        {
            var result = resolver.Check();
            
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
        ImGui.Checkbox("启用起手", ref MCHSettings.Instance.启用起手);
        _openerSelector.DrawCombo("起手选择", Openers);
    }

    internal void UpdatePlanning()
    {
        try { MachinistPlanning.Update(); }
        catch (Exception ex) { PlanningError(ex); }
        if (DebugLog.Enabled)
        {
            try { DebugLog.Frame(ReadDebugState()); }
            catch (Exception ex) { DebugLog.Fail(ex); }
        }
    }

    private void PlanningError(Exception ex)
    {
        var now = Environment.TickCount64;
        if (now - _lastPlanningError < 5000) return;
        _lastPlanningError = now;
        DebugLog.ObserveNote(now, $"规划异常：{ex.Message}");
        PluginLog.Warning($"[MCH] 规划异常：{ex.Message}");
    }

    private void Record(PAction? action, bool off, string reason)
    {
        if (!DebugLog.Enabled) return;
        try
        {
            var id = action?.ActionId ?? 0;
            DebugLog.Select(id, id == 0 ? 0 : id.GetAdjustedActionId(), Core.Target?.EntityId ?? 0, off, ReadDebugState(), reason);
        }
        catch (Exception ex) { DebugLog.Fail(ex); }
    }

    private static MachinistDebugState ReadDebugState()
    {
        var s = MachinistPlanning.ReadState();
        return new()
        {
            Now = s.Now, PlayerId = s.PlayerId, Alive = s.Alive, InCombat = s.InCombat, HasTarget = s.HasTarget,
            Gcd = s.Gcd, GcdLeft = s.GcdLeft,
            Context = $"MCH {GlobalVersion.Version} 等级={s.Level} 地图={Svc.ClientState.TerritoryType}",
            Qts = string.Join(" ", QtList.Keys.Select(qt => $"{qt}={PromeSettings.Instance.GetQt(qt)}")) +
                $" 启用起手={MCHSettings.Instance.启用起手}",
            Transition = $"过热={s.OverheatStacks} 野火效果={s.WildfireHits} 快速野火={s.FastWildfireActive} 窗口版本={s.WindowVersion}",
            Details = $"热/电={s.Heat}/{s.Battery} GCD={s.GcdLeft:F3}/{s.GcdTotal:F3} 插入={s.Weaves}/{s.WeaveLimit} " +
                $"钻头={s.Drill:F3} 锚/锯={s.AnchorCd:F2}/{s.SawCd:F2} 整备={s.Reassemble:F2} 免费超荷={s.FreeHypercharge:F2} " +
                $"飞轮/全金属预备={s.Excavator:F2}/{s.FullMetal:F2} 超荷CD={s.HyperchargeCd:F2} 连击={s.Combo}/{s.ComboLeft:F2} 下一段={s.ComboNext} " +
                $"动画锁={s.Lock:F3} 预测锁={s.ActionLock:F3} 插入预算={s.WeaveLock:F3} " +
                $"野火={s.WildfireLeft:F2}/{s.WildfireCd:F2} 枪管={s.BarrelCd:F2} 机器人={s.QueenLeft:F2} " +
                $"团辅={s.PartyLeft:F2} 团辅参考={s.PartyCycle?.ToString("F1") ?? "未观察"} 药={s.PotionLeft:F2} " +
                $"窗口={s.WindowLeft:F2} 留={s.GoalHeat}/{s.GoalBattery}｜{MachinistPlanning.Description}"
        };
    }
    
    private void DrawDev()
    {
        ImGui.BeginDisabled(DebugLog.Error.Length > 0);
        if (ImGui.Button(DebugLog.Enabled ? "停止记录日志" : "开始记录日志"))
        {
            try
            {
                if (DebugLog.Enabled) DebugLog.Disable(Environment.TickCount64);
                else DebugLog.Enable(ReadDebugState());
            }
            catch (Exception ex) { DebugLog.Fail(ex); }
        }
        ImGui.EndDisabled();
        if (DebugLog.Enabled) ImGui.TextUnformatted(DebugLog.FilePath.Length == 0 ? "已开启，等待开战" : "MCH日志记录中");
        if (DebugLog.FilePath.Length > 0) ImGui.TextUnformatted(DebugLog.FilePath);
        if (DebugLog.Error.Length > 0) ImGui.TextUnformatted(DebugLog.Error);
        ImGui.Separator();
        ImGui.TextWrapped(MachinistPlanning.Window.Describe(Environment.TickCount64));
        ImGui.TextWrapped(MachinistPlanning.Description);
        ImGui.Separator();
        var sheet = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        ImGui.Text($"整备冷却：{MCHSkill.整备.GetActionCooldown().ToString()}");
        ImGui.Text($"整备层数：{MCHSkill.整备.GetActionCharges().ToString()}");
        ImGui.Text($"整备处理层数：{MachinistHelper.GetCurrent整备Charge().ToString()}");
        ImGui.Text($"钻头冷却：{MCHSkill.钻头.GetActionCooldown().ToString()}");
        ImGui.Text($"钻头层数：{MCHSkill.钻头.GetActionCharges().ToString()}");
        ImGui.Text($"钻头处理层数：{MachinistHelper.GetCurrent钻头Charge().ToString()}");
        ImGui.Text($"{sheet.GetRowOrDefault(MachinistHelper.Get弹射CurrentId()).GetActionName()}：{MachinistHelper.Get弹射CurrentId().GetActionCharges()}");
        ImGui.Text($"{sheet.GetRowOrDefault(MachinistHelper.Get虹吸弹CurrentId()).GetActionName()}：{MachinistHelper.Get虹吸弹CurrentId().GetActionCharges()}");
        ImGui.Text($"超荷层数：{MachinistHelper.Get超荷Count()}");
        ImGui.Text($"GetActionCooldown");
        ImGui.Text(MachinistHelper.Get虹吸弹CurrentId().GetActionCooldown().ToString());
        ImGui.Text($"GetActionRecastTime");
        ImGui.Text(MachinistHelper.Get虹吸弹CurrentId().GetActionRecastTime().ToString());
        ImGui.Text($"GetActionRecastTimeElapsed");
        ImGui.Text(MachinistHelper.Get虹吸弹CurrentId().GetActionRecastTimeElapsed().ToString());
    }
    
}
