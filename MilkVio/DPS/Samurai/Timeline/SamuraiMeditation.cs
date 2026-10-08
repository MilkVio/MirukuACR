using ECommons.DalamudServices;
using ECommons.ExcelServices;
using MilkVio.DPS.Samurai.Level100;
using MilkVio.DPS.Samurai.SAMData;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Updaters;
using GameActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using GameActionType = FFXIVClientStructs.FFXIV.Client.Game.ActionType;

namespace MilkVio.DPS.Samurai.Timeline;

internal enum SamuraiMeditationMode { Always, NoEnemies, TargetHp }

internal static class SamuraiMeditation
{
    private enum Stage { Idle, Waiting, Preparing, Submitted, Active }
    private static Stage _stage;
    private static SamuraiMeditationMode _mode;
    private static string _comparison = "lte";
    private static float _hp;
    private static uint _player;
    private static uint _territory;
    private static long _deadline, _started;
    private static bool _seenStatus;
    public static bool Pending => _stage != Stage.Idle;
    public static bool Blocking => _stage == Stage.Preparing || _stage == Stage.Submitted || _stage == Stage.Active;
    public static string Status { get; private set; } = "没有默想请求";

    public static void Request(SamuraiMeditationMode mode, string comparison, float hp)
    {
        if (Blocking) return;
        if (Core.Me == null || Core.Me.IsDead || Core.Me.ClassJob.RowId != (uint)Job.SAM ||
            Core.Me.Level != 100 || !GameData.IsInCombat())
        { Svc.Chat.PrintError("[SAM] 默想请求仅支持百级武士战斗中使用"); return; }
        _mode = mode; _comparison = comparison; _hp = hp;
        _player = Core.Me.EntityId; _territory = Svc.ClientState.TerritoryType;
        _stage = Stage.Waiting; _seenStatus = false; _deadline = _started = 0;
        Note("默想等待条件");
        CheckCondition();
    }

    public static void Clear(string reason)
    {
        if (Pending) Note($"默想已清理：{reason}");
        _stage = Stage.Idle; _player = 0; _deadline = _started = 0; _seenStatus = false;
        Status = $"没有默想请求（{reason}）";
        Samurai100Planning.Invalidate();
    }

    public static void Update()
    {
        if (!Pending) return;
        try { UpdateCore(); }
        catch (Exception ex) { Fail($"默想异常：{ex.Message}"); }
    }

    private static unsafe void UpdateCore()
    {
        var me = Core.Me;
        if (me == null || me.EntityId != _player || me.IsDead || me.Level != 100 ||
            me.ClassJob.RowId != (uint)Job.SAM || !GameData.IsInCombat() || Svc.ClientState.TerritoryType != _territory)
        { Clear("死亡、脱战或切换角色区域"); return; }
        var now = Samurai100Planning.Now;
        if (_stage == Stage.Submitted || _stage == Stage.Active)
        {
            if (me.GetStatusLeftTime(SAMBuff.默想) > 0)
            {
                if (!_seenStatus) Note("默想中");
                _seenStatus = true; _stage = Stage.Active;
            }
            else if (_seenStatus) { Clear("默想结束"); return; }
            else if (now >= _deadline) { Fail("未确认默想生效"); return; }
            if (MoveManager.IsLocalPlayerMoving) { Clear("移动结束默想"); return; }
            if (now - _started >= 18000) Clear("默想保护到期");
            return;
        }
        if (!CheckCondition())
        {
            if (_stage == Stage.Preparing) { _stage = Stage.Waiting; Note("默想等待条件"); }
            return;
        }
        if (_stage == Stage.Waiting)
        {
            if (!ActionHelper.IsActionAvailableByLevelAndQuest(SAMSkill.默想) ||
                SAMSkill.默想.GetActionCooldown() > 0 || MoveManager.IsLocalPlayerMoving || me.IsCasting) return;
            _stage = Stage.Preparing;
            _deadline = now + (long)Math.Max(3000, 2 * Samurai100Helper.GcdSeconds * 1000 + 1000);
            Samurai100Planning.Invalidate();
            Note("默想等待当前动作结束");
        }
        if (now >= _deadline) { Fail("默想未找到可执行位置"); return; }
        if (MoveManager.IsLocalPlayerMoving) { Clear("准备期间移动"); return; }
        if (me.IsCasting || ActionHelper.GetAnimationLock() > 0 || ActionHelper.GetGcdRemain() > 0 ||
            ActionUpdater.HasActiveCommand() || ActionQueueManager.HasActionsInQueue() ||
            ActionQueueManager.GetQueueStatus().Values.Any(q => q.Count > 0)) return;
        var manager = GameActionManager.Instance();
        if (manager == null || manager->GetActionStatus(GameActionType.Action, SAMSkill.默想, me.GameObjectId) != 0) return;
        // 先保护，再单次提交；状态回包可能晚于技能效果。
        _stage = Stage.Submitted; _started = now; _deadline = now + 2500;
        Note("默想已提交，等待生效");
        ActionUpdater.UseAction(new PAction(SAMSkill.默想, ActionType.Always, ActionTargetType.Self) { NetworkTid = me.EntityId });
    }

    private static bool CheckCondition()
    {
        if (_mode == SamuraiMeditationMode.Always) return true;
        if (_mode == SamuraiMeditationMode.NoEnemies) return TargetHelper.IsAllBossUntargetable();
        var target = Core.Target;
        if (target == null || target.EntityId == Core.Me?.EntityId || target.MaxHp == 0)
        { Clear("血量检测没有有效目标"); return false; }
        var hp = 100f * target.CurrentHp / target.MaxHp;
        return _comparison switch
        {
            "gt" => hp > _hp, "gte" => hp >= _hp, "eq" => hp == _hp,
            "ne" => hp != _hp, "lt" => hp < _hp, "lte" => hp <= _hp, _ => false
        };
    }

    public static void ObserveEffect(ulong source, uint action)
    {
        if (_stage != Stage.Submitted || source != _player || action != SAMSkill.默想) return;
        _stage = Stage.Active;
        Note("默想效果已确认，等待状态");
    }

    private static void Note(string message) { Status = message; Samurai100Planning.WriteNote?.Invoke(message); }
    private static void Fail(string reason) { Clear(reason); Svc.Chat.PrintError($"[SAM] {reason}"); }
}
