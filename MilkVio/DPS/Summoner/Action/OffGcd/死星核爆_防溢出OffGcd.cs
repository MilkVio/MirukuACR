using ECommons;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.OffGcd;

public class 死星核爆_防溢出OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");

        //if (SMNSkill.星极超流.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        var draType = SummonerHelper.GetCurrent龙神类型();
        if (draType == 龙神类型.烈日龙神)
        {
            if (SMNSkill.烈日核爆.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        }
        if (draType == 龙神类型.龙神)
        {
            if (SMNSkill.死星核爆.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        }
        if (draType == 龙神类型.不死鸟)
        {
            if (SMNSkill.苏生之炎.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        }

        if (SummonerHelper.IsBahamutAttuned())
        {
            if (JobGaugeHelper.SMN.SummonTimerRemaining < 4500)
            {
                return new CheckResult(true, "防溢出打一个");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var draType = SummonerHelper.GetCurrent龙神类型();
        var party = PartyHelper.GetParty();
        if(draType == 龙神类型.不死鸟 && party.Count > 1)
        {
            return new PAction(SMNSkill.星极超流, ActionType.OffGcd, ActionTargetType.PartyMember2);    
        }
        return new PAction(SMNSkill.星极超流, ActionType.OffGcd, ActionTargetType.Target);
    }
}
