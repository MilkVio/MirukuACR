using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using MilkVio.Tank.WAR.WARData;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.Tank.WAR.Action.Gcd;
using MilkVio.Tank.WAR.Action.OffGcd;
using MilkVio.Tank.WAR.Opener;
using PromeRotation.Timeline;
using PromeRotation.Timeline.Core;
using PromeRotation.UI;
using PromeRotation.UI.HotKey;
using PromeRotation.UI.Hotkeys;

namespace MilkVio.Tank.WAR;

[RotationMetadata((uint)Job.WAR, "战士元始版", "MilkVio", GlobalVersion.Version, ContentScope = AcrContentScope.HighEnd)]
public class WarriorRotation : IRotation
{
    public static IJobNodeProvider? NodeProvider { get; } = new WarriorJobNodeProvider();
    // 创建一个属于该职业的回调
    private readonly IRotationEventHandler _eventHandler = new WarriorRotationEventHandler();
    public IRotationEventHandler GetEventHandler() => _eventHandler;
    
    // 管理该职业所有的决策解析器
    private readonly List<IDecisionResolver> _gcdResolvers = new();
    private readonly List<IDecisionResolver> _offGcdResolvers = new();
    
    // 实现对外暴露的静态属性
    // Qt列表
    public static IReadOnlyDictionary<string, bool> QtList { get; } = new Dictionary<string, bool>
    {
        {WARQt.启用起手, true},
        {WARQt.不打60, false},
        {WARQt.飞斧, true},
        {WARQt.猛攻, true},
        {WARQt.蛮荒崩裂, true},
        {WARQt.优先续红斩, true},
        {WARQt.只打绿斩, false},
        {WARQt.只打红斩, false},
        {WARQt.不溢出战嚎, true},
        {WARQt.攒资源, false},
        {WARQt.最终爆发, false},
        {WARQt.突进无位移, false},
    };
    // 起手列表
    public static IReadOnlyDictionary<string, Type> Openers { get; } = new Dictionary<string, Type>
    {
        {"战士绝亚DollSkip_MT起手", typeof(WAR_80_TEADS_MT)},
        {"战士绝欧MT起手", typeof(WAR_90_TOP_MT)},
        {"战士绝伊甸MT专用起手2", typeof(WAR_100_FRU_MT_FA)},
    };
    
    
    public WarriorRotation()
    {
        // 在这里，按照优先级从高到低的顺序，注册所有的解析器
        // 爆发相关的oGCD优先级最高
        _offGcdResolvers.Add(new 原初的解放OffGcd());
        _offGcdResolvers.Add(new 战嚎OffGcd());
        _offGcdResolvers.Add(new 动乱OffGcd());
        _offGcdResolvers.Add(new 原初的怒震OffGcd());
        _offGcdResolvers.Add(new 猛攻OffGcd());
        
        // 爆发状态下的GCD
        _gcdResolvers.Add(new 蛮荒崩裂Gcd());
        _gcdResolvers.Add(new 战嚎Gcd());
        _gcdResolvers.Add(new 尽毁Gcd());
        _gcdResolvers.Add(new 裂石飞环Gcd());
        _gcdResolvers.Add(new 基础Gcd());
        _gcdResolvers.Add(new 飞斧Gcd());
        
        // 画QT
        foreach (var (name, def) in QtList)
            PromeSettings.Instance.AddQt(name, def);
        
        // 画HotKey
        HotkeyUI.AddHotkey(new ActionHotkey(new PAction(WARSkill.猛攻, ActionType.OffGcd, ActionTargetType.Target)));
        HotkeyUI.AddHotkey(new ActionHotkey(new PAction(WARSkill.挑衅, ActionType.OffGcd, ActionTargetType.Target)));
        HotkeyUI.AddHotkey(new DelegateHotkey(
                               "退避ST",
                               new ExecuteLogic(() => 
                               {
                                   ActionQueueManager.Enqueue(new PAction(WARSkill.退避, ActionType.OffGcd, ActionTargetType.PartyMember2), true);
                               }),
                               iconActionId: DRKSkill.退避, 
                               customIconPath: "Resources/DRK/TBST.png" 
                           ));
        HotkeyUI.AddHotkey(new ActionHotkey(new PAction(WARSkill.原初的血气, ActionType.OffGcd, ActionTargetType.Self)));
        HotkeyUI.AddHotkey(new ActionHotkey(new PAction(WARSkill.战栗, ActionType.OffGcd, ActionTargetType.Self)));
        HotkeyUI.AddHotkey(new ActionHotkey(new PAction(WARSkill.铁壁, ActionType.OffGcd, ActionTargetType.Self)));
        HotkeyUI.AddHotkey(new ActionHotkey(new PAction(WARSkill.戮罪, ActionType.OffGcd, ActionTargetType.Self)));
        HotkeyUI.AddHotkey(new ActionHotkey(new PAction(WARSkill.亲疏自行, ActionType.OffGcd, ActionTargetType.Self)));
        HotkeyUI.AddHotkey(new ActionHotkey(new PAction(WARSkill.血仇, ActionType.OffGcd, ActionTargetType.Self)));
        HotkeyUI.AddHotkey(new ActionHotkey(new PAction(WARSkill.摆脱, ActionType.OffGcd, ActionTargetType.Self)));
        
        HotkeyUI.AddHotkey(new DelegateHotkey(
                               "勇猛ST",
                               new ExecuteLogic(() => 
                               {
                                   ActionQueueManager.Enqueue(new PAction(WARSkill.原初的勇猛, ActionType.OffGcd, ActionTargetType.PartyMember2), true);
                               }),
                               iconActionId: WARSkill.原初的勇猛, 
                               customIconPath: "Resources/DRK/HDST.png" 
                           ));
        
        HotkeyUI.AddHotkey(new ActionHotkey(new PAction(WARSkill.守护, ActionType.OffGcd, ActionTargetType.Self)));
        
        HotkeyUI.AddHotkey(new DelegateHotkey(
                               "同步镜头",
                               new ToggleLogic(
                                   getState: () => CameraSyncManager.CurrentMode == SyncMode.Camera, 
                                   toggleAction: () => CameraSyncManager.ToggleCameraSync()
                               ), 
                               iconActionId: 11404
                           ));

        // [新增] 注册全新的“正方向校准”Hotkey
        HotkeyUI.AddHotkey(new DelegateHotkey(
                               "校准正方向",
                               new ToggleLogic(
                                   getState: () => CameraSyncManager.CurrentMode == SyncMode.Align, 
                                   toggleAction: () => CameraSyncManager.ToggleAlignSync()
                               ),
                               customIconPath: "Resources/Align4.png"
                           ));
        
        HotkeyUI.AddHotkey(new DelegateHotkey(
                               "清扫队列",
                               new ExecuteLogic(() => 
                               {
                                   ActionQueueManager.ClearAllQueues();
                                   Svc.Chat.PrintError($"[PromeRotation] 清扫队列");
                               }), 
                               customIconPath: "Resources/Clear.png"
                           ));
    }
    
    // 该职业的起手
    public IOpener? GetOpener()
    {
        if (!PromeSettings.Instance.GetQt("启用起手") && Core.Me == null)
            return null;

        // PTL 起手优先：PTL 未提供 Opener 时才回退旧 Timeline Meta。
        var openerName = PromeRotation.PureTimeline.PtlManager.CurrentOpener;
        var openerSource = "PureTimeline";

        if (string.IsNullOrWhiteSpace(openerName))
        {
            var meta = TimelineManager.CurrentMeta;
            openerName = meta?.Opener;
            openerSource = "Timeline";
        }

        if (string.IsNullOrWhiteSpace(openerName))
            return null;
        
        // 查找对应起手类型（来自当前职业的 Meta.Openers）
        var openers = RotationManager.GetOpenersByJob((int)Core.Me.ClassJob.RowId);
        if (openers == null || !openers.TryGetValue(openerName, out var openerType))
        {
            PluginLog.Warning($"[ACR] {openerSource} 指定起手不存在：{openerName}");
            return null;
        }

        try
        {
            // 动态创建对应起手类实例
            if (Activator.CreateInstance(openerType) is IOpener opener)
            {
                PluginLog.Information($"[ACR] 从{openerSource} Meta 加载起手：{openerName}");
                return opener;
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error($"[ACR] 创建起手实例失败: {ex.Message}");
        }

        return null;
    }
    
    public PAction? NextGcd()
    {
        // 遍历所有GCD解析器
        foreach (var resolver in _gcdResolvers)
        {
            if (resolver.Check().Success)
            {
                // 找到第一个满足条件的，返回它的决策结果
                return resolver.GetAction();
            }
        }
        // 如果所有求解器都不满足条件，返回null
        return null;
    }
    
    public PAction? NextOffGcd()
    {
        // 遍历所有oGCD解析器 同上
        foreach (var resolver in _offGcdResolvers)
        {
            if (resolver.Check().Success)
            {
                return resolver.GetAction();
            }
        }
        return null;
    }
    
    public void UpdateDebugStatus()
    {
        // 清空上一帧的旧数据
        RotationManager.GcdSolverStatus.Clear();
        RotationManager.OffGcdSolverStatus.Clear();
        
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
        }
    }

    private void DrawGeneral()
    {
        
    }
    
    private void DrawDev()
    {
        var player = Core.Me;
        ImGui.Text($"红斩剩余时间：{player.GetStatusLeftTime(WARBuff.战场风暴)}");
        ImGui.Text($"战嚎剩余层数：{WARSkill.战嚎.GetActionCharges()}");
        ImGui.Text($"战嚎复唱时间：{WARSkill.战嚎.GetActionCooldown()}");
    }
    
}
