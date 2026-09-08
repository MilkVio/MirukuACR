using MilkVio.DPS.Reaper.ReaperData;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Updaters;

namespace MilkVio.DPS.Reaper.Action.OffGcd;

public class 真北OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ReaperQt.真北)) return new(false, "未开启真北");
        var me = Core.Me;
        var target = Core.Target;
        if (me == null || me.IsDead || target == null || target.IsDead || !target.IsTargetable
            || target.EntityId == me.EntityId || target.IsPlayer() || me.DistanceToMe() > GameData.GetCurrentMeleeRange())
            return new(false, "没有有效近战目标");
        var skill = UniversalData.MeleeUniversalSkill.真北;
        var charges = skill.GetActionCharges();
        if (!float.IsFinite(charges) || charges < 1 || !ActionHelper.IsActionAvailableByLevelAndQuest(skill))
            return new(false, "真北不可用");
        if (me.HasStatus(1250) || ReaperHelper.IsIn附体() || !ReaperHelper.IsIn妖异之镰())
            return new(false, "当前不需要真北");
        if (!TargetHelper.HasPositionalRequirement(target)) return new(false, "目标无身位要求");
        var next = ReaperBattleData.Instance.Planner.GcdAction;
        if (next != 0 && next != ReaperSkill.缢杀) return new(false, "下一招不是身位技");
        if (ReaperHelper.选择身位技能().Position == TargetHelper.GetTargetPositional()) return new(false, "身位匹配");

        var remaining = ActionHelper.GetGcdRemain();
        var elapsed = ActionHelper.GetGcdElapsed();
        var gcd = ReaperHelper.普通Gcd;
        var latest = Math.Max(0.8f, PromeSettings.Instance.Hacks.GcdQueueWindowSeconds + 0.15f);
        if (!gcd.HasValue || !float.IsFinite(remaining) || !float.IsFinite(elapsed)
            || elapsed < gcd.Value / 2 || remaining > Math.Min(1, gcd.Value / 2) || remaining < latest)
            return new(false, "等待复唱后半段的安全插入位置");
        if (ActionHelper.GetAnimationLock() > 0 || ActionQueueManager.HasActionsInOffGcdQueue()
            || ActionQueueManager.HasActionsInGcdQueue() || ActionQueueManager.HasHighPriorityAction()
            || ActionUpdater.HasActiveCommand() || ActionUpdater.HasLockedGcdAction())
            return new(false, "当前队列繁忙，跳过真北");
        return new(true, "为下一次身位技使用真北");
    }

    public PAction GetAction() => new(UniversalData.MeleeUniversalSkill.真北, ActionType.OffGcd, ActionTargetType.Self);
}
