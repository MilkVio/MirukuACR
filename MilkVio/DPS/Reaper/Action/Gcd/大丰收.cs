using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.Gcd;

public class 大丰收 : IDecisionResolver
{
    // 大丰收 消耗死亡祭品层数的射线AOE 与死亡祭祀Buff互锁（存在时无法发动）
    public CheckResult Check()
    {
        if (ReaperHelper.IsIn附体() || ReaperHelper.IsIn妖异之镰()) return new CheckResult(false, "当前在附体/妖异之镰");

        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (me.HasStatus(ReaperBuff.夜游魂衣预备Buff) || me.HasStatus(ReaperBuff.完人预备Buff))
            return new CheckResult(false, "先兑现已有免费附体/完人");

        if (me.HasStatus(ReaperBuff.死亡祭祀Buff))
        {
            return new CheckResult(false, "当前在死亡祭祀 收集中");
        }

        var 祭品层数 = me.GetStatusStackCount(ReaperBuff.死亡祭品Buff);
        if (祭品层数 < 1)
        {
            return new CheckResult(false, "没有死亡祭品层数");
        }

        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");

        if (me.DistanceToMe() > GameData.GetCurrentAttackRange(15)) return new CheckResult(false, "当前目标过远（>15m）");

        // 解锁后按已有层数使用，不等未出招/死亡的队友；Check不启动或推进等待计时。
        return new CheckResult(true, "死亡祭祀已结束 有祭品直接放");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.大丰收, ActionType.Gcd, ActionTargetType.Target);
    }
}
