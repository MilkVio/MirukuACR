using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Rotation;
using MilkVio.DPS.Samurai.Action.Gcd;
using MilkVio.DPS.Samurai.Action.OffGcd;
using MilkVio.DPS.Samurai.Opener;
using MilkVio.DPS.Samurai.SAMData;
using MilkVio.DPS.UniversalData;
using PromeRotation.Timeline;
using PromeRotation.UI;
using PromeRotation.UI.HotKey;
using PromeRotation.Updaters;
using PromeRotation.Windows;
using MilkVio.Common;
using MilkVio.DPS.Samurai.Level100;
using PromeRotation.Extensions;
using PromeRotation.Hosting;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Samurai;

[RotationMetadata((uint)Job.SAM, "团队型武士", "MilkVio", GlobalVersion.Version, ContentScope = AcrContentScope.HighEnd)]
public class SamuraiRotation : IRotation, IRotationLifecycle
{
    public static IJobNodeProvider NodeProvider { get; } = new SamuraiJobNodeProvider();
    // 创建一个属于该职业的回调
    private readonly IRotationEventHandler _eventHandler;
    public IRotationEventHandler GetEventHandler() => _eventHandler;
    
    // 管理该职业所有的决策解析器
    private readonly List<IDecisionResolver> _alwaysResolvers = new();
    private readonly List<IDecisionResolver> _gcdResolvers = new();
    private readonly List<IDecisionResolver> _offGcdResolvers = new();
    private readonly OpenerSelector _openerSelector = new();
    private readonly List<IDisposable> _subscriptions = new();
    private string _openerName = "宿主尚未请求起手";
    private readonly SamuraiPredictionHints _predictionHints = new();
    private bool _automaticPrediction;
    internal SamuraiPrediction Prediction { get; private set; }
    internal SamuraiDebugLog DebugLog { get; private set; } = CreateDebugLog();

    private static SamuraiDebugLog CreateDebugLog() => new(() => System.IO.Path.Combine(
        Svc.PluginInterface.ConfigDirectory.FullName, "ACR", "MilkVio", "DebugLog"));
    
    // 实现对外暴露的静态属性
    // Qt列表
    public static IReadOnlyDictionary<string, bool> QtList { get; } = new Dictionary<string, bool>
    {
        {SAMData.SAMQt.倾泻资源, false},
        {SAMData.SAMQt.AOE, false},
        {SAMData.SAMQt.不打120, false},
        {SAMData.SAMQt.闪影红莲, true},
        {SAMData.SAMQt.奥义斩浪, true},
        {SAMData.SAMQt.震天, true},
        {SAMData.SAMQt.残心, true},
        {SAMData.SAMQt.彼岸花, true},
        {SAMData.SAMQt.明镜止水, true},
        {SAMData.SAMQt.照破, true},
        {SAMData.SAMQt.燕飞, true},
        {SAMData.SAMQt.真北, true},
        {SAMData.SAMQt.立即回返, false},
    };
    // 起手列表
    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>
    {
        {"武士80-100月彼起手", typeof(SAM_80_100_月彼)},
        {"武士绝亚DollSkip起手", typeof(SAM_80_TEADS)},
        {"武士绝欧起手", typeof(SAM_90_OMG)},
    };
    
    public SamuraiRotation()
    {
        _eventHandler = new SamuraiRotationEventHandler(this);
        // 在这里，按照优先级从高到低的顺序，注册所有的解析器
        // 爆发相关的oGCD优先级最高
        _offGcdResolvers.Add(new 残心OffGcd());
        _offGcdResolvers.Add(new 意气冲天OffGcd());
        _offGcdResolvers.Add(new 照破OffGcd());
        _offGcdResolvers.Add(new 真北OffGcd(() => ResolveNextGcd(false)));
        _offGcdResolvers.Add(new 必杀剑_红莲OffGcd());
        _offGcdResolvers.Add(new 必杀剑_闪影OffGcd());
        _offGcdResolvers.Add(new 明镜止水OffGcd());
        _offGcdResolvers.Add(new 必杀剑_九天OffGcd());
        _offGcdResolvers.Add(new 必杀剑_震天OffGcd());
        
        // 爆发状态下的GCD
        _gcdResolvers.Add(new 奥义斩浪Gcd());
        _gcdResolvers.Add(new 燕回返Gcd());
        _gcdResolvers.Add(new 居合术Gcd());
        _gcdResolvers.Add(new 居合等待Gcd());
        _gcdResolvers.Add(new 明镜Gcd());
        _gcdResolvers.Add(new AOEGcd());
        _gcdResolvers.Add(new 雪月花闪连Gcd());
        _gcdResolvers.Add(new 连击1Gcd());
        _gcdResolvers.Add(new 燕飞Gcd());
        
        // 画QT
        foreach (var (name, def) in QtList)
            PromeSettings.Instance.AddQt(name, def);
        
        var hotkeyPanel = new HotkeyPanel(columns: 5, title: "SAM Hotkeys");
        hotkeyPanel.AddHotkey("牵制", new PAction(MeleeUniversalSkill.牵制, ActionType.OffGcd, ActionTargetType.Target));
        hotkeyPanel.AddHotkey("真北", new PAction(MeleeUniversalSkill.真北, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("心眼", new PAction(SAMSkill.心眼, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("疾跑", new PAction(3, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("亲疏自行", new PAction(MeleeUniversalSkill.亲疏自行, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("内丹", new PAction(MeleeUniversalSkill.内丹, ActionType.OffGcd, ActionTargetType.Self));
        hotkeyPanel.AddHotkey("浴血", new PAction(MeleeUniversalSkill.浴血, ActionType.OffGcd, ActionTargetType.Self));
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
                Samurai100Planning.Reset("手动清扫队列");
                Svc.Chat.PrintError("[PromeRotation] 清扫队列");
            }),
            customIconPath: "Resources/Clear.png");
        HotkeyManager.Instance.AddHotkeyPanel(hotkeyPanel);
    }
    
    // 该职业的起手
    public IOpener? GetOpener()
    {
        if (!SAMSettings.Instance.启用起手 || Core.Me == null)
            return null;

        ResetPrediction();
        var opener = _openerSelector.Resolve(Openers);
        _openerName = opener?.OpenerName ?? "无起手";
        if (DebugLog.Enabled)
        {
            try
            {
                var sequence = opener == null ? "无" : string.Join(" → ", opener.InCombatSequence.Select(a => SamuraiDebugLog.ActionName(a.ActionId)));
                DebugLog.ObserveNote(Environment.TickCount64, $"起手选择={_openerName} 候选序列={sequence}；是否入队由宿主决定，固定序列忽略QT");
            }
            catch (Exception ex) { DebugLog.Fail(ex); }
        }
        return opener;
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
        _automaticPrediction = true;
        UpdatePlanning();
        return ResolveNextGcd(true);
    }

    // 真北和诊断读取同一份选招，不记录第二次选择。
    private PAction? ResolveNextGcd(bool record)
    {
        var blocked = record && DebugLog.Enabled ? new List<string>() : null;
        // 遍历所有GCD解析器
        foreach (var resolver in _gcdResolvers)
        {
            var result = resolver.Check();
            if (result.Success)
            {
                // 找到第一个满足条件的，返回它的决策结果
                var action = resolver.GetAction();
                if (record) RecordSelection(action, false, $"{resolver.GetType().Name}：{result.Message}");
                return action;
            }
            blocked?.Add($"{resolver.GetType().Name}：{result.Message}");
        }
        // 如果所有求解器都不满足条件，返回null
        if (blocked != null) RecordSelection(null, false, string.Join("；", blocked));
        return null;
    }
    
    public PAction? NextOffGcd()
    {
        _automaticPrediction = true;
        UpdatePlanning();
        if (Samurai100Planning.TryUsePotionNow(Samurai100Planning.Now)) return null;
        var blocked = DebugLog.Enabled ? new List<string>() : null;
        // 遍历所有oGCD解析器 同上
        foreach (var resolver in _offGcdResolvers)
        {
            var result = resolver.Check();
            if (result.Success)
            {
                var action = resolver.GetAction();
                RecordSelection(action, true, $"{resolver.GetType().Name}：{result.Message}");
                return action;
            }
            blocked?.Add($"{resolver.GetType().Name}：{result.Message}");
        }
        if (blocked != null) RecordSelection(null, true, string.Join("；", blocked));
        return null;
    }

    private void RecordSelection(PAction? action, bool off, string reason)
    {
        if (!DebugLog.Enabled) return;
        try
        {
            var id = action?.ActionId ?? 0;
            DebugLog.Select(id, id == 0 ? 0 : id.GetAdjustedActionId(), Core.Target?.EntityId ?? 0,
                off, ReadDebugState(), reason);
        }
        catch (Exception ex) { DebugLog.Fail(ex); }
    }

    internal void UpdateDebugLog()
    {
        if (!DebugLog.Enabled) return;
        try { DebugLog.Frame(ReadDebugState()); }
        catch (Exception ex) { DebugLog.Fail(ex); }
    }

    internal void UpdatePlanning()
    {
        try { Samurai100Planning.Update(Samurai100Planning.Now); }
        catch (Exception ex)
        {
            Samurai100Planning.Reset("规划异常，基础循环接管");
            DebugLog.ObserveNote(Environment.TickCount64, $"规划异常：{ex.Message}");
        }
    }

    internal void ResetPrediction()
    {
        _automaticPrediction = false;
        Prediction = default;
        _predictionHints.Reset();
    }

    internal void UpdatePrediction()
    {
        var settings = SAMSettings.Instance;
        if (!settings.显示技能预测 && !settings.显示下G横幅)
        { Prediction = default; _predictionHints.Reset(); return; }
        if (!settings.显示下G横幅) _predictionHints.Reset();
        if (Core.Me == null || Core.Me.IsDead || !GameData.IsInCombat() || !Samurai100Helper.Enabled ||
            Core.Target == null || Core.Target.IsDead || !Core.Target.IsTargetable || Core.Target.EntityId == Core.Me.EntityId)
        { Prediction = default; _predictionHints.Reset(); return; }
        if (PromeSettings.Instance.EnableAcr != AcrState.On)
        { Prediction = new() { Waiting = "循环暂停" }; _predictionHints.Reset(); return; }
        // 固定起手和无法读取的宿主命令不冒充自动循环。
        if (!_automaticPrediction)
        { Prediction = new() { Waiting = "尚未接管" }; _predictionHints.Reset(); return; }
        if (Core.Me.IsCasting)
        {
            Prediction = new() { Action = Core.Me.CastActionId, Waiting = "正在读条" };
            return;
        }
        if (ActionQueueManager.HasActionsInQueue() || ActionUpdater.HasActiveCommand())
        { Prediction = new() { Waiting = "等待执行" }; return; }
        try
        {
            Prediction = SamuraiPrediction.Read(ResolveNextGcd(false)?.ActionId ?? 0);
            if (!settings.显示下G横幅) return;
            var hint = _predictionHints.Take(Prediction, Samurai100Planning.ReadState(), Samurai100Planning.Now);
            if (hint.Length == 0) return;
            HintHelper.ShowToast2(hint, 1.5f, HintHelper.HintType.Info);
            DebugLog.ObserveNote(Environment.TickCount64, $"预测横幅：{hint}");
        }
        catch (Exception ex)
        {
            Prediction = default; _predictionHints.Reset();
            DebugLog.ObserveNote(Environment.TickCount64, $"预测暂不可用：{ex.Message}");
        }
    }

    private void DrawPrediction()
    {
        try { SamuraiPredictionDisplay.Draw(Prediction); }
        catch (Exception ex)
        {
            SAMSettings.Instance.显示技能预测 = false;
            DebugLog.ObserveNote(Environment.TickCount64, $"预测窗口已关闭：{ex.Message}");
        }
    }

    private SamuraiDebugState ReadDebugState()
    {
        var now = Environment.TickCount64;
        var me = Core.Me;
        var target = Core.Target;
        var qts = string.Join(" ", QtList.Keys.Select(qt => $"{qt}={PromeSettings.Instance.GetQt(qt)}")) +
                  $" 启用起手={SAMSettings.Instance.启用起手} 预测窗={SAMSettings.Instance.显示技能预测} 横幅={SAMSettings.Instance.显示下G横幅}";
        var context = $"SAM {GlobalVersion.Version} 地图={Svc.ClientState.TerritoryType} 起手={_openerName}";
        if (me == null) return new SamuraiDebugState { Now = now, Qts = qts, Context = context, Transition = "无自身对象", Details = "无自身状态" };
        var single100 = Samurai100Helper.Enabled;
        var gcd = single100 ? Samurai100Helper.GcdSeconds : ActionHelper.GetGcdTotal();
        var potion = Samurai100Burst.PotionLeft;
        var party = Samurai100Burst.PartyBuffLeft;
        var validTarget = target != null && target.IsTargetable && !target.IsDead && target.EntityId != me.EntityId;
        var nextGcd = validTarget ? ResolveNextGcd(false)?.ActionId ?? 0 : 0;
        var details = $"等级={me.Level} 百级单体={single100} 目标={target?.EntityId:X} 距离={(target == null ? -1 : me.DistanceToMe()):F2} " +
            $"雪月花={JobGaugeHelper.SAM.HasYuki}/{JobGaugeHelper.SAM.HasMoon}/{JobGaugeHelper.SAM.HasHana} 剑气={JobGaugeHelper.SAM.剑气} 剑压={JobGaugeHelper.SAM.剑压} " +
            $"连击={ActionHelper.GetLastComboID()}/{ActionHelper.GetComboLeftTime():F3} GCD={gcd:F3}/{ActionHelper.GetGcdRemain():F3} " +
            $"咏唱ID={(me.IsCasting ? me.CastActionId : 0)} 移动={MoveManager.IsLocalPlayerMoving} " +
            $"下一刀={SamuraiDebugLog.ActionName(nextGcd)} 当前身位={TargetHelper.GetTargetPositional()} 所需身位={SamuraiHelper.GetNeedPositional(nextGcd)} " +
            $"目标有身位={(validTarget && TargetHelper.HasPositionalRequirement(target!))} 真北={me.GetStatusLeftTime(1250):F3} " +
            $"风月={me.GetStatusLeftTime(SAMBuff.风月):F3} 风花={me.GetStatusLeftTime(SAMBuff.风花):F3} " +
            $"自身花={SamuraiHelper.GetOwnHiganbanaLeftTime():F3} 花来源={me.EntityId:X} 花咏唱={Samurai100Helper.CastSeconds(SAMSkill.彼岸花):F3} " +
            $"明镜={SamuraiHelper.明镜止水层数():F3}/{me.GetStatusStackCount(SAMBuff.明镜止水)}/{me.GetStatusLeftTime(SAMBuff.明镜止水):F3} " +
            $"天道={me.GetStatusLeftTime(SAMBuff.天道):F3} 燕回返={SamuraiHelper.燕回返LeftTime():F3} " +
            $"奥义预备={me.GetStatusLeftTime(SAMBuff.奥义浪斩预备):F3} 回返斩浪={SamuraiHelper.回返斩浪可用()} 残心预备={me.GetStatusLeftTime(SAMBuff.残心预备):F3} " +
            $"意气CD={SAMSkill.意气冲天.GetActionCooldown():F3} 闪影CD={SAMSkill.必杀剑_闪影.GetActionCooldown():F3} 药={potion:F3} 团辅={party:F3} " +
            $"预测={SamuraiDebugLog.ActionName(Prediction.Action)}/{Prediction.Gcds}/{Prediction.Text}";
        if (single100)
            details += $" 留一闪={Samurai100Helper.KeepOneSenForHiganbana()} 预计居合={Samurai100Helper.GetBestIaijutsu()} " +
                       $"花预计生效={Samurai100Helper.HiganbanaTimeAfter(Samurai100Helper.GcdsToNextHiganbana()):F3} " +
                       $"剑气留用={Samurai100Burst.ReservedKenki()} 下刀剑气={Samurai100Burst.NextKenkiGain()} " +
                       $"规划={Samurai100Planning.Description} 用药={Samurai100Planning.Status}";
        return new SamuraiDebugState
        {
            Now = now, PlayerId = me.EntityId, Alive = !me.IsDead, InCombat = GameData.IsInCombat(), HasTarget = validTarget,
            Gcd = gcd, GcdLeft = ActionHelper.GetGcdRemain(), Context = context, Qts = qts, Details = details,
            Transition = $"等级={me.Level} 存活={!me.IsDead} 目标={target?.EntityId:X}/{validTarget} 移动={MoveManager.IsLocalPlayerMoving} 药中={potion > 0} 团辅中={party > 0}"
        };
    }

    public void OnEnterAcr()
    {
        OnExitAcr();
        DebugLog = CreateDebugLog();
        Samurai100Planning.WriteNote = note => DebugLog.ObserveNote(Environment.TickCount64, note);
        Samurai100Planning.Enter();
        Svc.PluginInterface.UiBuilder.Draw += DrawPrediction;
        _subscriptions.Add(PromeEventBus.OnActionEffect(this, e =>
        {
            Samurai100Planning.ObserveEffect(e.SourceId, e.ActionId, e.GlobalSequence, Environment.TickCount64);
            DebugLog.ObserveEffect(e.SourceId, e.TargetId, e.ActionId, e.GlobalSequence, Environment.TickCount64);
            if (SAMSettings.Instance.显示下G横幅)
                _predictionHints.Observe(e.SourceId, e.TargetId, e.ActionId, e.GlobalSequence, Samurai100Planning.Now);
        }));
        _subscriptions.Add(PromeEventBus.OnStartCast(this, e =>
        {
            if (Core.Me != null && e.SourceId == Core.Me.EntityId) _predictionHints.Reset();
            DebugLog.ObserveCast(e.SourceId, e.TargetId, e.ActionId, Environment.TickCount64);
        }));
        _subscriptions.Add(PromeEventBus.OnCancelCast(this, e =>
        {
            Samurai100Planning.ObserveCancel(e.SourceId);
            if (Core.Me != null && e.SourceId == Core.Me.EntityId) _predictionHints.Reset();
            DebugLog.ObserveCast(e.SourceId, 0, e.ActionId, Environment.TickCount64, "取消咏唱");
        }));
        _subscriptions.Add(PromeEventBus.OnInterruptCast(this, e =>
        {
            Samurai100Planning.ObserveCancel(e.SourceId);
            if (Core.Me != null && e.SourceId == Core.Me.EntityId) _predictionHints.Reset();
            DebugLog.ObserveCast(e.SourceId, 0, e.ActionId, Environment.TickCount64, "中断咏唱");
        }));
        _subscriptions.Add(PromeEventBus.OnStatusAdd(this, e =>
            DebugLog.ObserveHiganbana(e.SourceId, e.TargetId, e.StatusId, e.Duration, e.IsRefresh ? "刷新" : "添加", Environment.TickCount64)));
        _subscriptions.Add(PromeEventBus.OnStatusRemove(this, e =>
            DebugLog.ObserveHiganbana(e.SourceId, e.TargetId, e.StatusId, e.Duration, "移除", Environment.TickCount64)));
        _subscriptions.Add(PromeEventBus.OnCountdownStarted(this, () =>
        { ResetPrediction(); DebugLog.ObserveNote(Environment.TickCount64, "倒计时开始"); }));
        _subscriptions.Add(PromeEventBus.OnPlayerDied(this, () =>
        {
            Samurai100Planning.Reset("死亡");
            ResetPrediction();
            DebugLog.ObserveNote(Environment.TickCount64, "死亡");
        }));
        _subscriptions.Add(PromeEventBus.OnPlayerRevived(this, () => DebugLog.ObserveNote(Environment.TickCount64, "复活")));
    }

    public void OnExitAcr()
    {
        Svc.PluginInterface.UiBuilder.Draw -= DrawPrediction;
        ResetPrediction();
        Samurai100Planning.Exit();
        foreach (var subscription in _subscriptions) subscription.Dispose();
        _subscriptions.Clear();
        DebugLog.Shutdown(Environment.TickCount64);
        _openerName = "宿主尚未请求起手";
    }
    
    public void UpdateDebugStatus()
    {
        // 清空上一帧的旧数据
        RotationManager.AlwaysSolverStatus.Clear();
        RotationManager.GcdSolverStatus.Clear();
        RotationManager.OffGcdSolverStatus.Clear();

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
        ImGui.Checkbox("启用起手", ref SAMSettings.Instance.启用起手);
        _openerSelector.DrawCombo("起手选择", Openers);
        ImGui.Separator();
        ImGui.Checkbox("显示技能预测", ref SAMSettings.Instance.显示技能预测);
        ImGui.Checkbox("显示下G横幅（1.5秒）", ref SAMSettings.Instance.显示下G横幅);
        ImGui.TextDisabled("仅百级单体接管后提示，固定起手期间不预测。");
        if (SAMSettings.Instance.显示技能预测)
        {
            ImGui.Checkbox("锁定预测窗口", ref SAMSettings.Instance.锁定预测窗口);
            ImGui.SliderFloat("预测图标大小", ref SAMSettings.Instance.预测图标大小, 48, 144, "%.0f");
            if (ImGui.Button("重置预测位置")) SAMSettings.Instance.重置预测位置 = true;
        }
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
        if (DebugLog.Enabled) ImGui.TextUnformatted(DebugLog.FilePath.Length == 0 ? "已开启，等待开战" : "SAM日志记录中");
        if (DebugLog.FilePath.Length > 0) ImGui.TextUnformatted(DebugLog.FilePath);
        if (DebugLog.Error.Length > 0) ImGui.TextUnformatted(DebugLog.Error);
        ImGui.Separator();
        ImGui.Text($"当前预测：{Prediction.Text} {SamuraiDebugLog.ActionName(Prediction.Action)}");
        if (Samurai100Helper.Enabled)
        {
            ImGui.TextUnformatted(Samurai100Planning.Description);
            ImGui.TextUnformatted(Samurai100Planning.Status);
            ImGui.Text($"百级单体：GCD {Samurai100Helper.GcdSeconds:F2}秒，彼岸花咏唱 {Samurai100Helper.CastSeconds(SAMSkill.彼岸花):F2}秒");
            ImGui.Text($"有效连击：{Samurai100Helper.GetComboId()}，自身彼岸花 {Samurai100Helper.HiganbanaLeft:F2}秒");
            ImGui.Text($"留一闪续花：{Samurai100Helper.KeepOneSenForHiganbana()}，预计居合：{Samurai100Helper.GetBestIaijutsu()}");
            ImGui.Text($"药效 {Samurai100Burst.PotionLeft:F1}秒，已检测团辅 {Samurai100Burst.PartyBuffLeft:F1}秒，生效余量 {Samurai100Helper.EffectMargin:F1}秒");
        }
        ImGui.Text($"剑气：{JobGaugeHelper.SAM.剑气}");
        ImGui.Text($"剑压：{JobGaugeHelper.SAM.剑压}");
        ImGui.Text($"雪：{JobGaugeHelper.SAM.HasYuki}");
        ImGui.Text($"月：{JobGaugeHelper.SAM.HasMoon}");
        ImGui.Text($"花：{JobGaugeHelper.SAM.HasHana}");
        ImGui.Text($"回返斩浪可用：{SamuraiHelper.回返斩浪可用()}");
        ImGui.Text($"当前可用居合：{SamuraiHelper.GetCurrent居合类型().ToString()}");
        if (!Samurai100Helper.Enabled)
            ImGui.Text($"当前求解最佳技能：{SamuraiHelper.GetBestComboType().ToString()}");
        ImGui.Text($"明镜层数：{SamuraiHelper.明镜止水层数()}");
        ImGui.Text($"当前需要的身位：{SamuraiHelper.GetNeedPositional(ResolveNextGcd(false)?.ActionId ?? 0)}");
        
        ImGui.Text($"必杀剑_红莲:{ActionHelper.IsActionAvailableByLevelAndQuest(SAMSkill.必杀剑_红莲).ToString()}");
        ImGui.Text($"照破:{ActionHelper.IsActionAvailableByLevelAndQuest(SAMSkill.照破).ToString()}");
        ImGui.Text($"残心:{ActionHelper.IsActionAvailableByLevelAndQuest(SAMSkill.残心).ToString()}");
    }
    
}
