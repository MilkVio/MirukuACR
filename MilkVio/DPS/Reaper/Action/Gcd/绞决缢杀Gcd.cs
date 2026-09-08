using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.Gcd;

public class 绞决缢杀Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        var me = Core.Me;
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, $"当前目标过远（>{currentMeleeRange}m）");
        if (ReaperHelper.IsIn附体()) return new CheckResult(false, "当前在附体");

        if (ReaperHelper.IsIn妖异之镰()) return new CheckResult(true, "当前在妖异之镰");
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        // 由游戏读取处刑变化，不依赖高亮是否及时出现。
        return new PAction(ReaperHelper.选择身位技能().Skill, ActionType.Gcd, ActionTargetType.Target);
    }
}
