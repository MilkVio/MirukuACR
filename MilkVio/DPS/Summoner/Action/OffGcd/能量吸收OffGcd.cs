using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.OffGcd;

public class 能量吸收OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");

        if (SMNSkill.能量吸收.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        if (JobGaugeHelper.SMN.AetherflowStacks != 0) return new CheckResult(false, "还有豆子");

        if (!PromeSettings.Instance.GetQt(SMNQt.不打120))
        {
            if (SMNSkill.灼热之光.GetActionCooldown() < 10)
            {
                return new CheckResult(false, "等团辅");
            }
        }
        return new CheckResult(true, "打一个");
    }

    public PAction GetAction()
    {
        return new PAction(SMNSkill.能量吸收, ActionType.OffGcd, ActionTargetType.Target);
    }
}
