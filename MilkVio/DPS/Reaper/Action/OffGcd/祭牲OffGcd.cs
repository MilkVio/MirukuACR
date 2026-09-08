using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.OffGcd;

public class 祭牲OffGcd : IDecisionResolver
{
    // 附体之后给的伤害技能 随手直接打掉
    public CheckResult Check()
    {
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (!ReaperHelper.IsIn附体()) return new CheckResult(false, "当前不在附体");
        if (!me.HasStatus(ReaperBuff.祭牲预备Buff)) return new CheckResult(false, "没有祭牲预备");

        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");

        if (me.DistanceToMe() > GameData.GetCurrentAttackRange(25)) return new CheckResult(false, "当前目标过远（>25m）");

        return new CheckResult(true, "有祭牲预备 随手打掉");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.祭性, ActionType.OffGcd, ActionTargetType.Target);
    }
}
