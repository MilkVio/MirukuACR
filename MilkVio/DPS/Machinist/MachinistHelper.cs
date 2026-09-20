using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.DPS.Machinist.MCHData;
using GameActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;

namespace MilkVio.DPS.Machinist;

public static class MachinistHelper
{
    // 当前引用的 SDK 尚未暴露此属性，按宿主运行时版本读写。
    private static readonly System.Reflection.PropertyInfo? MaxWeavesSetting = typeof(HackSettings).GetProperty("MaxOgcdsPerGcd");
    private static readonly MachinistWeaveLimiter WeaveLimiter = new();
    private static bool _weaveLimitEnabled;
    private static bool _weaveSettingWarningLogged;

    public static void EnterWeaveLimit()
    {
        _weaveLimitEnabled = true;
        ResetWeaveLimit();
        UpdateWeaveLimit();
    }

    public static void ExitWeaveLimit()
    {
        _weaveLimitEnabled = false;
        ResetWeaveLimit();
    }

    public static void ResetWeaveLimit()
    {
        WeaveLimiter.Reset();
        SetMaxWeaves(2);
    }

    public static unsafe void UpdateWeaveLimit()
    {
        if (!_weaveLimitEnabled) return;
        var me = Core.Me;
        if (me == null || me.IsDead || me.ClassJob.RowId != (uint)Job.MCH || !GameData.IsInCombat()
            || Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51])
        {
            ResetWeaveLimit();
            return;
        }

        var manager = GameActionManager.Instance();
        var recast = manager == null ? null : manager->GetRecastGroupDetail(57);
        if (recast == null)
        {
            ResetWeaveLimit();
            return;
        }

        SetMaxWeaves(WeaveLimiter.Update(Environment.TickCount64,
            me.HasStatus(MCHStatus.过热), me.GetStatusLeftTime(MCHStatus.过热),
            recast->IsActive, recast->ActionId, recast->Total, recast->Elapsed));
    }

    private static void SetMaxWeaves(int count)
    {
        if (MaxWeavesSetting is not { CanRead: true, CanWrite: true } || MaxWeavesSetting.PropertyType != typeof(int))
        {
            WarnWeaveSetting("宿主未提供可写的 MaxOgcdsPerGcd 设置");
            return;
        }
        try
        {
            var settings = PromeSettings.Instance.Hacks;
            if (MaxWeavesSetting.GetValue(settings) is int current && current != count)
                MaxWeavesSetting.SetValue(settings, count);
        }
        catch (Exception ex)
        {
            WarnWeaveSetting(ex.Message);
        }
    }

    private static void WarnWeaveSetting(string message)
    {
        if (_weaveSettingWarningLogged) return;
        _weaveSettingWarningLogged = true;
        PluginLog.Warning($"[Machinist] 无法设置过热能力技上限：{message}");
    }

    public static PAction GetBaseAction()
    {
        var lastComboId = ActionHelper.GetLastComboID();
        switch (lastComboId)
        {
            case MCHSkill.分裂弹1:
                return new PAction(MCHSkill.热独头弹2, ActionType.Gcd, ActionTargetType.Target);
            case MCHSkill.独头弹2:
                return new PAction(MCHSkill.热狙击弹3, ActionType.Gcd, ActionTargetType.Target);
            default:
                return new PAction(MCHSkill.热分裂弹1, ActionType.Gcd, ActionTargetType.Target);
        }
    }

    public static uint Get空气锚CurrentId()
    {
        var level = Core.Me.Level;
        if (level < 76) return MCHSkill.热弹;
        return MCHSkill.空气锚;
    }
    
    public static uint Get虹吸弹CurrentId()
    {
        var level = Core.Me.Level;
        if (level < 92) return MCHSkill.虹吸弹;
        return MCHSkill.双将;
    }
    
    public static uint Get弹射CurrentId()
    {
        var level = Core.Me.Level;
        if (level < 92) return MCHSkill.弹射;
        return MCHSkill.将死;
    }
    
    public static float GetCurrent整备Charge()
    {
        var player = Core.Me;
        float MaxCharges = 2f;  // 最大层数
        float PerCharge  = 55f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = MCHSkill.整备.GetActionCooldown();
        
        // 等级适配
        if (player.Level < 84)
        {
            MaxCharges = 1f;
            //cdToFull -= 1 * PerCharge;
        }
        
        // 防止出界（如果小于0或大于80）
        if (cdToFull < 0f) cdToFull = 0f;
        if (cdToFull > MaxCharges * PerCharge) cdToFull = MaxCharges * PerCharge;

        // 计算浮
        float real = MaxCharges - (cdToFull / PerCharge);
        if (real < 0f) real = 0f;
        if (real > MaxCharges) real = MaxCharges;

        return real;
    }
    
    public static float GetCurrent钻头Charge()
    {
        var player = Core.Me;
        float MaxCharges = 2f;  // 最大层数
        float PerCharge  = 20f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = MCHSkill.钻头.GetActionCooldown();
        
        // 等级适配
        if (player.Level < 94)
        {
            MaxCharges = 1f;
            //cdToFull -= 1 * PerCharge;
        }
        
        // 防止出界（如果小于0或大于80）
        if (cdToFull < 0f) cdToFull = 0f;
        if (cdToFull > MaxCharges * PerCharge) cdToFull = MaxCharges * PerCharge;

        // 计算浮
        float real = MaxCharges - (cdToFull / PerCharge);
        if (real < 0f) real = 0f;
        if (real > MaxCharges) real = MaxCharges;

        return real;
    }

    public static uint Get超荷Count()
    {
        var player = Core.Me;
        var heat = JobGaugeHelper.MCH.Heat;
        var count = 0;
        if (player.HasStatus(MCHStatus.超荷预备)) count += 1;
        if (heat >= 50 && heat != 100)
        {
            count += 1;
        }

        if (heat == 100)
        {
            count += 2;
        }
        
        return (uint)count;
    }

    public static uint GetCurrentRobotActionId()
    {
        var level = Core.Me.Level;
        if (level < 80)
        {
            return MCHSkill.车式浮空炮塔;
        }

        return MCHSkill.后式自走人偶;
    }
    
    public static uint GetCurrentRobotBurstActionId()
    {
        var level = Core.Me.Level;
        if (level < 80)
        {
            return MCHSkill.超档车式炮塔;
        }

        return MCHSkill.超档车式炮塔;
    }
}
