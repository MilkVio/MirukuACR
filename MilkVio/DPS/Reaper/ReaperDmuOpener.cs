using System.Collections.Concurrent;
using Dalamud.Game.ClientState.Objects.Enums;
using MilkVio.DPS.Reaper.ReaperData;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Updaters;

namespace MilkVio.DPS.Reaper;

internal sealed class ReaperDmuOpener
{
    // 仅重试从未开始的预读；限频且最多三次，不重试位移或被打断的咏唱。
    private const int HarpeRetryIntervalMs = 300;
    private const int HarpeMaxAttempts = 3;
    private const int CombatVerificationRetries = 5;
    private static readonly uint[] Skills = { ReaperSkill.勾刃, ReaperSkill.神秘环, ReaperSkill.地狱入境 };
    private readonly ConcurrentQueue<(uint Id, long At)> _effects = new();
    private int _step;
    private int _harpeAttempts;
    private ulong _playerId, _targetId;
    private long _armedAt, _pullAt, _startedAt, _stepAt, _submittedAt, _castFinishAt;
    private bool _countdown, _sawCast, _instantHarpe, _ownsHeading, _combatQueued;
    private float _harpeAt;
    public bool Active { get; private set; }
    public string Status { get; private set; } = "";

    public void BeginCountdown() => Start(true, Environment.TickCount64);

    public void BeginCombat()
    {
        if (!Active && !PromeSettings.Instance.OpenerHasBeenExecuted) Start(false, Environment.TickCount64);
        PromeSettings.Instance.OpenerHasBeenExecuted = true;
    }

    internal void Start(bool countdown, long now)
    {
        Reset();
        var me = Core.Me;
        if (me == null || me.IsDead || me.Level < 100 || me.ClassJob.RowId != 39 || !Permissions()) return;
        if (ReaperHelper.IsIn附体() || ReaperHelper.IsIn妖异之镰()) return;
        _playerId = me.GameObjectId;
        _armedAt = now;
        _countdown = countdown;
        // 和标准起手一样，在倒数开始时确定本次预读时间，避免途中修改挪动已安排的动作。
        _harpeAt = ReaperSettings.Instance.勾刃倒数预读时间;
        if (countdown) PromeSettings.Instance.OpenerHasBeenExecuted = false;
        _pullAt = now + (long)(Math.Max(0, GameData.GetCountdown()) * 1000);
        Active = true;
        Status = countdown ? "妖星起手：等待预读" : "妖星起手：勾刃";
        ReaperBattleData.Instance.DebugLog.Note(now, Status);
        ReaperBattleData.Instance.Planner.Replan("妖星专用起手接管");
    }

    public void EnqueueAction(ulong source, uint id, long at)
    {
        if (Active && !_combatQueued && source == _playerId && _effects.Count < 24 && Skills.Contains(id))
            _effects.Enqueue((id, at));
    }

    public void Reset(string reason = "")
    {
        var wasActive = Active;
        Active = false;
        ReleaseHeading();
        _effects.Clear();
        _step = _harpeAttempts = 0; _playerId = _targetId = 0;
        _armedAt = _pullAt = _startedAt = _stepAt = _submittedAt = _castFinishAt = 0;
        _countdown = _sawCast = _instantHarpe = _combatQueued = false;
        _harpeAt = 0;
        Status = reason;
        if (wasActive && !GameData.IsInCombat()) ReaperBattleData.Instance.DebugLog.CancelCountdown();
        if (wasActive && reason.Length > 0)
        {
            PromeSettings.Instance.OpenerHasBeenExecuted = true;
            ReaperBattleData.Instance.DebugLog.Note(Environment.TickCount64, reason);
        }
        if (wasActive) ReaperBattleData.Instance.Planner.Replan(reason);
    }

    public void Tick() => Tick(Environment.TickCount64);

    internal void Tick(long now)
    {
        if (!Active) return;
        try { Update(now); }
        catch (Exception)
        {
            Reset("妖星起手异常，交回普通循环");
            ReaperBattleData.Instance.Planner.Replan(Status);
        }
    }

    private void Update(long now)
    {
        var me = Core.Me;
        if (me == null || me.IsDead || me.GameObjectId != _playerId || me.ClassJob.RowId != 39)
        { Reset("妖星起手已取消"); return; }

        // 后三招交给宿主推进；Hold也由宿主暂停/恢复，不重排已提交的技能组。
        if (_combatQueued)
        {
            if (!QueueBusy()) Reset("妖星起手技能组结束，交回ACR求解");
            return;
        }
        if (!Permissions() || PromeSettings.Instance.EnableAcr is AcrState.Off or AcrState.Hold)
        { Reset("妖星起手已取消"); return; }

        var engaged = GameData.IsInCombat() || Core.Target is { } target && target.StatusFlags.HasFlag(StatusFlags.InCombat);
        var remaining = GameData.GetCountdown();
        if (_countdown && remaining <= 0 && !engaged
            && (_startedAt == 0 || now < _pullAt - 300 || now > _pullAt + 3000))
        { Reset("倒数取消，结束妖星起手"); return; }
        if (_startedAt > 0 && (now - _startedAt > 20000 || now - _stepAt > 5000))
        { Reset("妖星起手等待超时，交回普通循环"); return; }
        if (_targetId != 0 && (Core.Target?.GameObjectId != _targetId || Core.Target.IsDead || !Core.Target.IsTargetable))
        { Reset("目标变化，结束妖星起手"); return; }

        while (_effects.TryDequeue(out var effect))
        {
            if (effect.At >= _armedAt && effect.Id == ReaperSkill.地狱入境
                && (_step < 2 || _step == 2 && _submittedAt == 0))
            { Reset("已手动地狱入境，结束妖星起手，避免重复位移"); return; }
            if (_submittedAt > 0 && effect.At >= _submittedAt && effect.At >= _armedAt && effect.Id == Skills[_step])
            {
                Advance(now);
                if (_combatQueued) return;
            }
        }

        var id = Skills[_step];
        if (_submittedAt > 0)
        {
            if (id == ReaperSkill.勾刃 && me.IsCasting && me.CastActionId == id)
            {
                _sawCast = true;
                _castFinishAt = now + (long)(ActionHelper.GetCastTimeRemain() * 1000);
                Status = "妖星起手：勾刃咏唱中，后续顺序已准备";
                return;
            }
            // 勾刃伤害包可能晚于读条结束；以实际读条完成衔接能力技。
            if (id == ReaperSkill.勾刃 && _sawCast && !me.IsCasting)
            {
                if (now < _castFinishAt - 100) { Reset("勾刃读条中断，结束妖星起手"); return; }
                Advance(now);
            }
            else if (IsOffGcd(id) ? id.GetActionCooldown() > 0
                : ReaperHelper.最近公共技能() == id && ActionHelper.GetGcdElapsed() <= (now - _submittedAt) / 1000f + 0.1f
                    && (id != ReaperSkill.勾刃 || _instantHarpe))
                Advance(now);
            else
            {
                if (id == ReaperSkill.勾刃 && !_sawCast && !me.IsCasting && ActionHelper.GetGcdRemain() <= 0
                    && _harpeAttempts < HarpeMaxAttempts && now - _submittedAt >= HarpeRetryIntervalMs)
                    _submittedAt = 0;
                else
                {
                    if (now - _submittedAt > (id == ReaperSkill.地狱入境 ? 1200 : 2500))
                        Reset("技能未确认，结束妖星起手");
                    return;
                }
            }
            if (!Active || _combatQueued) return;
            id = Skills[_step];
        }

        if (_step == 0 && _countdown && !engaged && remaining > _harpeAt) return;
        if (_startedAt == 0) _startedAt = _stepAt = now;
        if (me.IsCasting || GameData.IsPlayerOccupied() || ActionHelper.GetAnimationLock() > 0 || QueueBusy()) return;

        var off = IsOffGcd(id);
        // 即时执行入口不负责宿主的提前预排，GCD真正转好后才提交。
        if (!off && ActionHelper.GetGcdRemain() > 0) return;
        var action = new PAction(id, off ? ActionType.OffGcd : ActionType.Gcd,
            id is ReaperSkill.神秘环 or ReaperSkill.地狱入境 ? ActionTargetType.Self : ActionTargetType.Target);
        if (!ReaperHelper.当前可执行(action)) return;
        if (_targetId == 0) _targetId = Core.Target!.GameObjectId;

        if (id == ReaperSkill.地狱入境)
        {
            _ownsHeading = true;
            CameraSyncManager.SetAngle(MathF.PI, 1500);
        }
        _submittedAt = now;
        if (id == ReaperSkill.勾刃)
        {
            _harpeAttempts++;
            _instantHarpe = me.HasStatus(ReaperBuff.勾刃效果提高Buff);
            // 预读即使被取消，也不能在随后伤害引战时重新开始同一套位移起手。
            PromeSettings.Instance.OpenerHasBeenExecuted = true;
        }
        Status = $"妖星起手：{Name(id)}，等待确认";
        // 使用宿主立即执行入口，尤其不能把位移留在普通队列中迟到执行。
        ReaperRotation.RecordRequest(action, Status);
        ActionUpdater.UseAction(action);
        ActionHelper.RecordAction(id);
    }

    private void Advance(long now)
    {
        if (Skills[_step] == ReaperSkill.地狱入境) ReleaseHeading();
        _step++;
        _submittedAt = 0; _stepAt = now;
        if (_step >= Skills.Length)
        {
            var sequence = new List<PAction>
            {
                new(ReaperSkill.死亡之影, ActionType.Gcd, ActionTargetType.Target)
                { RequiresVerification = true },
                new(ReaperSkill.灵魂切割, ActionType.Gcd, ActionTargetType.Target)
                { RequiresVerification = true },
                new(ReaperSkill.暴食, ActionType.OffGcd, ActionTargetType.Target)
                { RequiresVerification = true }
            };
            // 当前SDK尚未导出这个属性，宿主1.5.9.8已支持；只在入队时设置。
            var retries = typeof(PAction).GetProperty("MaxVerificationRetries");
            foreach (var action in sequence) retries?.SetValue(action, CombatVerificationRetries);
            ActionQueueManager.Enqueue(sequence);
            _combatQueued = true;
            _effects.Clear();
            Status = "妖星起手：宿主技能组（死亡之影→灵魂切割→暴食）";
            ReaperBattleData.Instance.DebugLog.Note(now, retries == null
                ? $"{Status}，当前宿主使用默认重试次数"
                : $"{Status}，每招最多重试{CombatVerificationRetries}次");
        }
        else Status = $"妖星起手：{Name(Skills[_step])}";
    }

    private void ReleaseHeading()
    {
        if (!_ownsHeading) return;
        _ownsHeading = false;
        try { CameraSyncManager.StopAngle(); }
        catch (Exception) { } // 宿主1500ms期限仍可解除面向。
    }

    private static bool Permissions() => ReaperSettings.Instance.启用起手;

    private static bool QueueBusy() => ActionQueueManager.HasActionsInGcdQueue() || ActionQueueManager.HasActionsInOffGcdQueue()
        || ActionQueueManager.HasActionsInAlwaysQueue() || ActionQueueManager.HasHighPriorityAction()
        || ActionUpdater.HasActiveCommand() || ActionUpdater.HasLockedGcdAction();

    private static bool IsOffGcd(uint id) => id is ReaperSkill.神秘环 or ReaperSkill.地狱入境;
    private static string Name(uint id) => id switch
    {
        ReaperSkill.勾刃 => "勾刃", ReaperSkill.神秘环 => "神秘环", ReaperSkill.地狱入境 => "地狱入境", _ => id.ToString()
    };
}
