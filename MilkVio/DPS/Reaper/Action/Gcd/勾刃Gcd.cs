using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.Gcd;

public class 勾刃Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ReaperQt.勾刃)) return new CheckResult(false, "未开启勾刃");
        if (ReaperHelper.IsIn附体() || ReaperHelper.IsIn妖异之镰())
            return new CheckResult(false, "当前在附体/妖异之镰");

        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");

        if (Core.Me.DistanceToMe() <= currentMeleeRange) return new CheckResult(false, "当前在近战距离内");
        if (Core.Me.DistanceToMe() > GameData.GetCurrentAttackRange(25)) return new CheckResult(false, "当前目标过远（>25m）");

        // 没有勾刃效果提高Buff需要读条 移动中不打
        if (!Core.Me.HasStatus(ReaperBuff.勾刃效果提高Buff) && MoveManager.IsLocalPlayerMoving)
            return new CheckResult(false, "勾刃需要读条 当前正在移动");

        return new CheckResult(true, "远离近战距离 打勾刃");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.勾刃, ActionType.Gcd, ActionTargetType.Target);
    }
}
