using Dalamud.Game.ClientState.Objects.Enums;
using MilkVio.DPS.Reaper.ReaperData;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Updaters;

namespace MilkVio.DPS.Reaper;

internal sealed class ReaperAutoSoulsow
{
    // 保留最后5秒给预读起手；至少等半秒，避免倒数一响就出手。
    private const float StopBeforePullSeconds = 5;
    private const float MinimumDelaySeconds = 0.5f;
    private long _useAt, _expiresAt;
    private ulong _playerId;
    private volatile bool _observedUse;
    public bool Active { get; private set; }

    public void Begin() => Begin(Environment.TickCount64, Random.Shared.NextDouble());

    internal void Begin(long now, double random)
    {
        Cancel();
        try
        {
            var me = Core.Me;
            var remaining = GameData.GetCountdown();
            var available = remaining - StopBeforePullSeconds;
            if (!ReaperSettings.Instance.倒计时自动播魂 || me == null || me.IsDead || me.ClassJob.RowId != 39
                || InCombat() || PromeSettings.Instance.EnableAcr is AcrState.Off or AcrState.Hold
                || me.HasStatus(ReaperBuff.播魂种Buff) || me.IsCasting && me.CastActionId == ReaperSkill.播魂种
                || !float.IsFinite(remaining) || available <= MinimumDelaySeconds || !double.IsFinite(random)) return;
            _playerId = me.GameObjectId;
            _observedUse = false;
            _expiresAt = now + (long)(available * 1000);
            _useAt = now + (long)((MinimumDelaySeconds + Math.Clamp(random, 0, 1) * (available - MinimumDelaySeconds)) * 1000);
            Active = true;
        }
        catch (Exception) { Cancel(); }
    }

    public void Cancel()
    {
        Active = false;
        _useAt = _expiresAt = 0;
        _playerId = 0;
        _observedUse = false;
    }

    public void ObserveAction(ulong source, uint id)
    {
        if (Active && source == _playerId && id == ReaperSkill.播魂种) _observedUse = true;
    }

    public void Tick() => Tick(Environment.TickCount64);

    internal void Tick(long now)
    {
        if (!Active) return;
        try
        {
            var me = Core.Me;
            var remaining = GameData.GetCountdown();
            if (!ReaperSettings.Instance.倒计时自动播魂 || me == null || me.IsDead || me.GameObjectId != _playerId
                || me.ClassJob.RowId != 39 || InCombat() || _observedUse
                || PromeSettings.Instance.EnableAcr is AcrState.Off or AcrState.Hold
                || !float.IsFinite(remaining) || remaining <= StopBeforePullSeconds || now >= _expiresAt
                || me.HasStatus(ReaperBuff.播魂种Buff) || me.IsCasting && me.CastActionId == ReaperSkill.播魂种)
            { Cancel(); return; }

            if (now < _useAt || me.IsCasting || GameData.IsPlayerOccupied()
                || ActionQueueManager.HasActionsInGcdQueue() || ActionQueueManager.HasActionsInOffGcdQueue()
                || ActionQueueManager.HasActionsInAlwaysQueue() || ActionQueueManager.HasHighPriorityAction()
                || ActionUpdater.HasActiveCommand() || ActionUpdater.HasLockedGcdAction()) return;

            var gcd = ActionHelper.GetGcdRemain();
            var animationLock = ActionHelper.GetAnimationLock();
            if (!float.IsFinite(gcd) || gcd != 0 || !float.IsFinite(animationLock) || animationLock != 0) return;

            var action = new PAction(ReaperSkill.播魂种, ActionType.Gcd, ActionTargetType.Self);
            if (!ReaperHelper.当前可执行(action)) return;
            // 只尝试一次即时执行，未成功也不留重试；Hold/死亡后本轮倒数不会重新安排。
            Cancel();
            ReaperRotation.RecordRequest(action, "倒数随机播魂");
            ActionUpdater.UseAction(action);
            ActionHelper.RecordAction(action.ActionId);
        }
        catch (Exception) { Cancel(); }
    }

    private static bool InCombat() => GameData.IsInCombat()
        || Core.Target is { } target && target.StatusFlags.HasFlag(StatusFlags.InCombat);
}
