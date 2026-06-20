using ECommons.DalamudServices;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;

namespace MilkVio.DPS.Dancer.Action.Gcd;

public class 团辅剑舞溢出Gcd : IDecisionResolver //12 强化12 大舞小舞结束动作落幕舞
{
    // 检查什么时候可以用这个技能
    // 如果检查通过 就执行下面的getaction
    public CheckResult Check()
    {
        var 伶俐 = JobGaugeHelper.DNC.Esprit;
        var gcd和目标距离 = GameData.GetCurrentAttackRange(25f);
        var me = Core.Me;

        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        if (Core.Me.DistanceToMe() <= gcd和目标距离)
        {
            //伶俐大于95优先释放
            if (伶俐 >= 85 && me.HasStatus(DNCBuff.技巧舞步结束))
            {
                return new CheckResult(true, "120");
            }
        }

        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {

        return new PAction(DNCSkill.剑舞, ActionType.Gcd, ActionTargetType.Target);

    }
}
