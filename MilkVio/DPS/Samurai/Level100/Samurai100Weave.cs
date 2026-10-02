using MilkVio.DPS.Samurai.SAMData;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Updaters;

namespace MilkVio.DPS.Samurai.Level100;

// GCD与能力技共用的短期资源安排。
internal static class Samurai100Weave
{
    public static float CastTime(Samurai100State s, uint action) =>
        action == SAMSkill.彼岸花 || action == SAMSkill.纷乱雪月花 ? s.Cast : action == SAMSkill.奥义斩浪 ? s.OgiCast : 0;

    public static bool ShohaReady(Samurai100State s) => s.UseShoha && s.Meditation == 3 &&
        s.ShohaCd <= 0 && s.Distance <= s.ShohaRange;

    public static bool SeneiReady(Samurai100State s) => s.UseSenei && s.SeneiCd <= 0 &&
        s.Kenki >= 25 && s.Moon > 0 && s.Distance <= s.MeleeRange;

    private static bool IkiWindow(Samurai100State s) => s.UseIki && s.Ogi <= 0 && !s.OgiReturn &&
        s.Zanshin <= 0 && s.Distance <= s.OgiRange && (!s.UseShinten || s.Kenki <= 50);

    public static uint BeforeCast(Samurai100State s, uint action, bool beforePotion)
    {
        var cast = CastTime(s, action);
        if (cast <= 0 || s.Distance > s.MeleeRange) return action;
        var needSlot = ShohaReady(s) || !beforePotion &&
            (IkiWindow(s) && s.IkiCd < cast + Samurai100Helper.EffectMargin ||
             s.UseSenei && s.Kenki >= 25 && s.Moon > 0 && s.SeneiCd < cast + Samurai100Helper.EffectMargin);
        if (!needSlot) return action;
        var filler = Samurai100Rules.SafePadding(s);
        // 奥义前也可收合法月花；不能添第二闪挡住即将到期的彼岸花。
        if (filler == 0 && action == SAMSkill.奥义斩浪 &&
            !(s.UseDot && s.SenCount == 1 && s.Dot <= 7 * s.Gcd + s.Cast))
        {
            if (s.MirrorStacks > 0) filler = Samurai100Rules.ChooseMoonFlower(s);
            else if (s.ComboLeft > s.GcdLeft + Samurai100Helper.EffectMargin)
            {
                if (s.Combo == SAMSkill.阵风 && (s.Sen & 2) == 0) filler = SAMSkill.月光;
                if (s.Combo == SAMSkill.士风 && (s.Sen & 4) == 0) filler = SAMSkill.花车;
            }
        }
        if (filler == 0) return action;
        // 续花和已有预备不能为创造穿插位过期。
        if (action == SAMSkill.彼岸花 && s.Dot <= s.Gcd + cast + Samurai100Helper.EffectMargin) return action;
        if (action == SAMSkill.奥义斩浪 && s.Ogi <= s.Gcd + cast + Samurai100Helper.EffectMargin) return action;
        return filler;
    }

    public static bool ShohaUrgent(Samurai100State s) => ShohaReady(s) &&
        CastTime(s, Samurai100Projection.NextResourceGcd(s)) > 0;

    // 留出即将转好的能力技位置，不用震天、真北把它占掉。
    public static float CooldownDelay(Samurai100State s)
    {
        var delay = float.PositiveInfinity;
        if (IkiWindow(s) && s.IkiCd > 0) delay = s.IkiCd;
        if (s.UseSenei && s.SeneiCd > 0 && s.Kenki >= 25 && s.Moon > 0 && s.Distance <= s.MeleeRange)
            delay = Math.Min(delay, s.SeneiCd);
        return delay < Samurai100Projection.AbilityLock &&
            delay + Samurai100Projection.AbilityLock <= s.GcdLeft ? delay : float.PositiveInfinity;
    }

    public static float ReadyIn(Samurai100State s)
    {
        var delay = float.PositiveInfinity;
        var ready = s; ready.IkiCd = 0;
        if (Samurai100Rules.CanIkishoten(ready, out _)) delay = s.IkiCd;
        if (s.UseSenei && s.Kenki >= 25 && s.Moon > s.SeneiCd && s.Distance <= s.MeleeRange)
            delay = Math.Min(delay, s.SeneiCd);
        return delay;
    }

    public static uint Emergency(Samurai100State s, uint next, bool beforePotion, bool tail = false)
    {
        if (s.Casting || s.GcdLeft >= Samurai100Projection.AbilityLock) return 0;
        var cast = CastTime(s, next) > 0;
        if (cast && ShohaReady(s)) return SAMSkill.照破;
        // 当前GCD末尾已放不下正常穿插，不能每轮都等到下一刀。
        if (beforePotion || !cast && next != 0 && !tail && s.GcdLeft <= 0) return 0;
        if (Samurai100Rules.CanIkishoten(s, out _)) return SAMSkill.意气冲天;
        if (SeneiReady(s)) return SAMSkill.必杀剑_闪影;
        return 0;
    }

    public static bool TryEmergency(out uint action, out string reason, bool nextGcd = false)
    {
        action = 0; reason = "";
        if (!Samurai100Helper.Enabled || !GameData.IsInCombat() || Core.Me == null ||
            Core.Me.IsDead || Core.Me.IsCasting || Core.Target == null || Core.Target.IsDead || !Core.Target.IsTargetable ||
            Core.Target.EntityId == Core.Me.EntityId || PromeSettings.Instance.EnableAcr != AcrState.On ||
            ActionQueueManager.HasHighPriorityAction()) return false;
        var s = Samurai100Planning.ReadState();
        if (s.GcdLeft >= Samurai100Projection.AbilityLock) return false;
        var next = Samurai100Planning.TryGcd(out var planned, out _) ? planned : Samurai100Projection.NextResourceGcd(s);
        var tail = s.GcdLeft > 0;
        if (nextGcd) s = Samurai100Projection.AtNextGcd(s);
        action = Emergency(s, next, Samurai100Planning.PotionPending, tail);
        if (action == 0) return false;
        reason = action == SAMSkill.照破 ? "满剑压且不能安全垫刀，先照破再读条" :
            $"{SamuraiDebugLog.ActionName(action)}已就绪，当前无正常穿插位";
        return true;
    }

    public static PAction? AlwaysAction(out string reason)
    {
        reason = "";
        if (ActionHelper.GetAnimationLock() > 0 || ActionUpdater.HasActiveCommand() ||
            !TryEmergency(out var id, out reason)) return null;
        var currentTarget = Core.Target;
        var currentPlayer = Core.Me;
        if (currentTarget == null || currentPlayer == null) return null;
        var action = new PAction(id, ActionType.Always, id == SAMSkill.意气冲天 ? ActionTargetType.Self : ActionTargetType.Target);
        // PVE没有派发复查：固定目标，单次尝试，失败后按实际状态重算。
        action.NetworkTid = id == SAMSkill.意气冲天 ? currentPlayer.EntityId : currentTarget.EntityId;
        return action;
    }
}
