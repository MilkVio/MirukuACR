using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.Gcd;

public class 星极脉冲Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        // 决策不允许的条件
        if (!SummonerHelper.IsBahamutAttuned()) return new CheckResult(false, "当前没有龙神召唤");
        
        return new CheckResult(true, "打一个");
    }

    public PAction? GetAction()
    {
        var 龙神类型 = SummonerHelper.GetCurrent龙神类型();
        if (龙神类型 == 龙神类型.None) return null;
        switch (龙神类型)
        {
            case 龙神类型.烈日龙神:
                return new PAction(SMNSkill.灵极脉冲, ActionType.Gcd, ActionTargetType.Target);
            case 龙神类型.龙神:
                return new PAction(SMNSkill.星极脉冲, ActionType.Gcd, ActionTargetType.Target);
            case 龙神类型.不死鸟:
                return new PAction(SMNSkill.灵泉之炎, ActionType.Gcd, ActionTargetType.Target);
            default:
                return null;
        }
    }
}
