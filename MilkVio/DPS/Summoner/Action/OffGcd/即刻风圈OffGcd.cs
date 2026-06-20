using System.Collections.Generic;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;
using MilkVio.DPS.UniversalData;

namespace MilkVio.DPS.Summoner.Action.OffGcd;

public class 即刻风圈OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(SMNQt.即刻风圈)) return new CheckResult(false, "未开启即刻风圈");
        
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        if (SummonerHelper.IsGarudaDone(me)) return new CheckResult(false, "当前不在风神"); 
        
        if (PromeSettings.Instance.GetQt(SMNQt.龙神召唤))
        {
            if (SMNSkill.龙神召唤.GetActionCooldown() <= 1) return new CheckResult(false, "当前有龙神召唤");
        }
        
        if (me.HasStatus(SMNBuff.螺旋气流预备) && SMNSkill.即刻咏唱.GetActionCooldown() == 0)
        {
            return new CheckResult(true, "打即刻风圈");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        List<PAction> 即刻风圈 = new List<PAction>
        {
            new(MageUniversalSkill.即刻咏唱, ActionType.OffGcd, ActionTargetType.Self),
            new(SMNSkill.螺旋气流, ActionType.Gcd, ActionTargetType.Target),
        };
        ActionQueueManager.EnqueueOffGcdList(即刻风圈);
        // 不返回任何技能，因为 Group 会自动开始执行
        return null;
    }
}
