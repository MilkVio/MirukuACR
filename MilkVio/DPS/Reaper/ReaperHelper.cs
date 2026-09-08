using PromeRotation.Extensions;
using MilkVio.DPS.Reaper.ReaperData;
using PromeRotation.Helpers;
using PromeRotation.Data;
using GameActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using GameActionType = FFXIVClientStructs.FFXIV.Client.Game.ActionType;

namespace MilkVio.DPS.Reaper;

public static class ReaperHelper
{
    public static bool QtAllows(uint id) => id switch
    {
        ReaperSkill.神秘环 => PromeSettings.Instance.GetQt(ReaperQt.神秘环),
        ReaperSkill.夜游魂衣 => PromeSettings.Instance.GetQt(ReaperQt.附体),
        ReaperSkill.暴食 => PromeSettings.Instance.GetQt(ReaperQt.暴食),
        ReaperSkill.隐匿挥割 or ReaperSkill.绞决爪 or ReaperSkill.缢杀爪 => PromeSettings.Instance.GetQt(ReaperQt.隐匿挥割),
        ReaperSkill.灵魂切割 or ReaperSkill.灵魂钐割 => PromeSettings.Instance.GetQt(ReaperQt.灵魂割),
        ReaperSkill.死亡之影 or ReaperSkill.死亡之涡 => PromeSettings.Instance.GetQt(ReaperQt.Dot),
        ReaperSkill.收获月 => PromeSettings.Instance.GetQt(ReaperQt.收获月),
        ReaperSkill.勾刃 => PromeSettings.Instance.GetQt(ReaperQt.勾刃),
        var skill when skill == UniversalData.MeleeUniversalSkill.真北 => PromeSettings.Instance.GetQt(ReaperQt.真北),
        _ => true
    };
    public static float? 普通Gcd => 读取复唱时间(ReaperSkill.切割);
    public static float? 附体Gcd => 读取复唱时间(ReaperSkill.虚无收割);

    public static unsafe float? 读取咏唱时间(uint id)
    {
        if (Core.Me == null || GameActionManager.Instance() == null) return null;
        try
        {
            var seconds = GameActionManager.GetAdjustedCastTime(GameActionType.Action, id) / 1000f;
            return seconds >= 0 && seconds <= 10 ? seconds : null;
        }
        catch (Exception) { return null; }
    }

    public static unsafe uint 最近公共技能()
    {
        var manager = GameActionManager.Instance();
        if (manager == null) return 0;
        var recast = manager->GetRecastGroupDetail(57);
        return recast == null ? 0 : recast->ActionId;
    }

    public static unsafe float 灵魂切单层复唱()
    {
        if (Core.Me == null || GameActionManager.Instance() == null) return 30;
        var seconds = GameActionManager.GetAdjustedRecastTime(GameActionType.Action, ReaperSkill.灵魂切割) / 1000f;
        return seconds > 0 && seconds <= 60 ? seconds : 30;
    }

    public static float 自身死亡烙印剩余()
    {
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || target == null) return 0;
        foreach (var status in target.StatusList)
            if (status.StatusId == ReaperBuff.死亡烙印 && status.SourceId == me.EntityId)
                return Math.Abs(status.RemainingTime);
        return 0;
    }

    public static float 自身神秘环剩余()
    {
        var me = Core.Me;
        if (me == null) return 0;
        foreach (var status in me.StatusList)
            if (status.StatusId == ReaperBuff.神秘环Buff && status.SourceId == me.EntityId)
                return Math.Abs(status.RemainingTime);
        return 0;
    }

    // 查询技能本身的修正复唱，不能用最近一次技能留下的公共冷却总长。
    public static unsafe float? 读取复唱时间(uint actionId)
    {
        if (Core.Me == null || Core.Me.ClassJob.RowId != 39 || GameActionManager.Instance() == null)
            return null;

        try
        {
            var seconds = GameActionManager.GetAdjustedRecastTime(GameActionType.Action, actionId) / 1000f;
            return float.IsFinite(seconds) && seconds > 0 && seconds <= 10 ? seconds : null;
        }
        catch (Exception)
        {
            // 可选的时序数据读不到时，界面显示未读取，基础循环继续。
            return null;
        }
    }

    public static uint 下一段连击()
    {
        if (Core.Me == null) return 0;
        var remaining = ActionHelper.GetComboLeftTime();
        if (!float.IsFinite(remaining) || remaining <= ActionHelper.GetGcdRemain() + 0.1f) return 0;

        var next = ActionHelper.GetLastComboID() switch
        {
            ReaperSkill.切割 => ReaperSkill.增盈切割,
            ReaperSkill.增盈切割 => ReaperSkill.地狱切割,
            _ => 0u
        };
        return next != 0 && ActionHelper.IsActionAvailableByLevelAndQuest(next) ? next : 0;
    }

    // 延后若干秒才续连击是否会断；已经来不及救的连击不能反过来封锁资源技能。
    public static bool 需要续连击(float? delay)
    {
        if (!delay.HasValue || !float.IsFinite(delay.Value) || delay.Value <= 0) return false;
        if (IsIn附体() || IsIn妖异之镰() || 下一段连击() == 0) return false;
        if (!有近战目标()) return false;

        return ActionHelper.GetComboLeftTime() <= ActionHelper.GetGcdRemain() + delay.Value + 0.5f;
    }

    public static float? 单附体占用时间()
    {
        var me = Core.Me;
        if (me == null) return null;
        var reap = 附体Gcd;
        if (!ActionHelper.IsActionAvailableByLevelAndQuest(ReaperSkill.团契)) return 5 * reap;

        var duration = 4 * reap + 读取复唱时间(ReaperSkill.团契);
        if (me.HasStatus(ReaperBuff.补完Buff))
            duration += 读取复唱时间(ReaperSkill.完人);
        return duration;
    }

    public static bool 需要提前续连击()
    {
        var me = Core.Me;
        if (me == null) return false;
        var gcd = 普通Gcd;
        if (需要续连击(gcd)) return true;

        if (PromeSettings.Instance.GetQt(ReaperQt.附体)
            && ActionHelper.IsActionAvailableByLevelAndQuest(ReaperSkill.夜游魂衣)
            && ReaperSkill.夜游魂衣.GetActionCooldown() <= 0
            && 可用附体层数() > 0
            && !me.HasStatus(ReaperBuff.完人预备Buff)
            && 需要续连击(单附体占用时间())) return true;

        return PromeSettings.Instance.GetQt(ReaperQt.暴食)
            && ActionHelper.IsActionAvailableByLevelAndQuest(ReaperSkill.暴食)
            && ReaperSkill.暴食.GetActionCooldown() <= 0
            && JobGaugeHelper.RPR.灵魂值 >= 50
            && 需要续连击(2 * gcd);
    }

    public static bool 有近战目标()
    {
        var me = Core.Me;
        var target = Core.Target;
        return me != null && !me.IsDead && target != null && !target.IsDead && target.IsTargetable
            && target.EntityId != me.EntityId && !target.IsPlayer()
            && me.DistanceToMe() <= GameData.GetCurrentMeleeRange();
    }

    // GCD预排时公共冷却还没结束，所以只让游戏检查目标、资源及技能状态。
    // 能力技仍必须已冷却，避免不可用的高优先级动作占住宿主队列。
    public static bool 当前可执行(PAction action) => 当前可执行(action, out _);

    public static unsafe bool 当前可执行(PAction action, out uint? nativeStatus)
    {
        nativeStatus = null;
        var me = Core.Me;
        var manager = GameActionManager.Instance();
        if (action.ActionId == 0 || me == null || me.IsDead || manager == null) return false;
        var target = action.Target == ActionTargetType.Self ? me : Core.Target;
        if (target == null || target.IsDead || !target.IsTargetable) return false;
        if (action.Target != ActionTargetType.Self)
        {
            var range = action.ActionId switch
            {
                ReaperSkill.大丰收 => GameData.GetCurrentAttackRange(15),
                ReaperSkill.团契 or ReaperSkill.完人 or ReaperSkill.勾刃 or ReaperSkill.收获月
                    or ReaperSkill.暴食 or ReaperSkill.祭性 => GameData.GetCurrentAttackRange(25),
                _ => GameData.GetCurrentMeleeRange()
            };
            if (me.DistanceToMe() > range) return false;
        }
        if (!ActionHelper.IsActionAvailableByLevelAndQuest(action.ActionId)) return false;
        if (action.ActionId == UniversalData.MeleeUniversalSkill.真北)
        {
            var charges = action.ActionId.GetActionCharges();
            if (!float.IsFinite(charges) || charges < 1) return false;
        }
        else if (action.Type == ActionType.OffGcd)
        {
            var cooldown = action.ActionId.GetActionCooldown();
            if (!float.IsFinite(cooldown) || cooldown > 0) return false;
        }
        if (action.ActionId == ReaperSkill.灵魂切割 && 灵魂切层数() < 1) return false;

        nativeStatus = manager->GetActionStatus(GameActionType.Action, action.ActionId, target.GameObjectId,
            checkRecastActive: false, checkCastingActive: false);
        return nativeStatus == 0;
    }

    public static (uint Skill, Positional Position) 选择身位技能()
    {
        var me = Core.Me;
        var gibbet = me != null && me.HasStatus(ReaperBuff.绞决效果提高Buff);
        var gallows = me != null && me.HasStatus(ReaperBuff.缢杀效果提高Buff);
        var useGibbet = gibbet != gallows ? gibbet
            : Core.Target != null && TargetHelper.HasPositionalRequirement(Core.Target)
                && TargetHelper.GetTargetPositional() == Positional.Flank;
        var skill = useGibbet ? ReaperSkill.绞决 : ReaperSkill.缢杀;
        return (skill.GetAdjustedActionId(), useGibbet ? Positional.Flank : Positional.Rear);
    }

    public static bool IsIn附体() => 附体剩余时间() > 0;

    public static float 附体剩余时间()
    {
        var me = Core.Me;
        if (me == null || me.IsDead || me.ClassJob.RowId != 39) return 0;
        // 特殊复苏可能清除Buff，但量谱里的附体仍在计时；量谱单位为毫秒。
        var gaugeLeft = JobGaugeHelper.RPR.夜游魂衣剩余时间 / 1000f;
        return Math.Max(gaugeLeft, me.GetStatusLeftTime(ReaperBuff.夜游魂衣Buff));
    }
    
    public static bool IsIn妖异之镰()
    {
        var me = Core.Me;
        if (me == null) return false;
        return me.HasStatus(ReaperBuff.处刑人Buff) || me.HasStatus(ReaperBuff.妖异之镰Buff) ;
    }

    public static uint 可用附体层数()
    {
        if (Core.Me == null) return 0;
        var gauge = JobGaugeHelper.RPR.魂衣值;
        return (uint)gauge / 50 + (Core.Me.HasStatus(ReaperBuff.夜游魂衣预备Buff) ? 1u : 0u);
    }
    
    public static float 灵魂切层数()
    {
        var player = Core.Me;
        if (player == null || player.Level < 60) return 0;
        float MaxCharges = 2f;  // 最大层数
        float PerCharge = 灵魂切单层复唱();
        // 返回距离充满还剩多少秒
        float cdToFull = ReaperSkill.灵魂切割.GetActionCooldown();
        if (!float.IsFinite(cdToFull)) return 0;
        
        // 等级适配
        if (player.Level < 78)
        {
            MaxCharges = 1f;
        }
        
        // 防止出界
        if (cdToFull < 0f) cdToFull = 0f;
        if (cdToFull > MaxCharges * PerCharge) cdToFull = MaxCharges * PerCharge;

        // 计算
        float real = MaxCharges - (cdToFull / PerCharge);
        if (real < 0f) real = 0f;
        if (real > MaxCharges) real = MaxCharges;

        return real;
    }
}
