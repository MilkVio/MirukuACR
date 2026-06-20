using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.Gcd;

public class 三神召唤Gcd : IDecisionResolver
{
    public 召唤类型 NextSummon = 召唤类型.None;
    public CheckResult Check()
    {
        if (PromeSettings.Instance.GetQt(SMNQt.不打三神)) return new CheckResult(false, "已开启不打三神");
        
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        // 自身不可以的条件
        NextSummon = 召唤类型.None;
        // 决策不允许的条件
        if (JobGaugeHelper.SMN.SummonTimerRemaining != 0) return new CheckResult(false, "当前召唤物未消失");
        
        if (PromeSettings.Instance.GetQt(SMNQt.龙神召唤))
        {
            if (SMNSkill.龙神召唤.GetActionCooldown() <= 1) return new CheckResult(false, "当前有龙神召唤");
        }

        if (PromeSettings.Instance.GetQt(SMNQt.快打三神))
        {
            if (PromeSettings.Instance.GetQt(SMNQt.优先土神) && JobGaugeHelper.SMN.IsTitanReady)
            {
                NextSummon = 召唤类型.土神;
                return new CheckResult(true, "快打三神_优先打土神");
            }
            
            if (PromeSettings.Instance.GetQt(SMNQt.优先风神) && JobGaugeHelper.SMN.IsGarudaReady)
            {
                NextSummon = 召唤类型.风神;
                return new CheckResult(true, "快打三神_优先打风神");
            }
            
            if (PromeSettings.Instance.GetQt(SMNQt.优先火神) && JobGaugeHelper.SMN.IsIfritReady)
            {
                NextSummon = 召唤类型.火神;
                return new CheckResult(true, "快打三神_优先打火神");
            }
            
            if (JobGaugeHelper.SMN.IsTitanReady)
            {
                NextSummon = 召唤类型.土神;
                return new CheckResult(true, "快打三神_打土神");
            }
            
            if (JobGaugeHelper.SMN.IsGarudaReady)
            {
                NextSummon = 召唤类型.风神;
                return new CheckResult(true, "快打三神_打风神");
            }
            
            if (JobGaugeHelper.SMN.IsIfritReady)
            {
                NextSummon = 召唤类型.火神;
                return new CheckResult(true, "快打三神_打火神");
            }
        }
        else
        {
            if (!SummonerHelper.IsTitanDone(me)) return new CheckResult(false, "当前正在土神召唤");
            if (!SummonerHelper.IsGarudaDone(me)) return new CheckResult(false, "当前正在风神召唤");
            if (!SummonerHelper.IsIfritDone(me)) return new CheckResult(false, "当前正在火神召唤"); 
            
            if (PromeSettings.Instance.GetQt(SMNQt.优先土神) && JobGaugeHelper.SMN.IsTitanReady)
            {
                NextSummon = 召唤类型.土神;
                return new CheckResult(true, "普通三神_优先打土神");
            }
            
            if (PromeSettings.Instance.GetQt(SMNQt.优先风神) && JobGaugeHelper.SMN.IsGarudaReady)
            {
                NextSummon = 召唤类型.风神;
                return new CheckResult(true, "普通三神_优先打风神");
            }
            
            if (PromeSettings.Instance.GetQt(SMNQt.优先火神) && JobGaugeHelper.SMN.IsIfritReady)
            {
                NextSummon = 召唤类型.火神;
                return new CheckResult(true, "普通三神_优先打火神");
            }
            
            if (JobGaugeHelper.SMN.IsTitanReady)
            {
                NextSummon = 召唤类型.土神;
                return new CheckResult(true, "普通三神_打土神");
            }
            
            if (JobGaugeHelper.SMN.IsGarudaReady)
            {
                NextSummon = 召唤类型.风神;
                return new CheckResult(true, "普通三神_打风神");
            }
            
            if (JobGaugeHelper.SMN.IsIfritReady)
            {
                NextSummon = 召唤类型.火神;
                return new CheckResult(true, "普通三神_打火神");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction? GetAction()
    {
        
        if (NextSummon == 召唤类型.None) return null;
        if (NextSummon == 召唤类型.土神) return new PAction(SMNSkill.土神召唤, ActionType.Gcd, ActionTargetType.Target);
        if (NextSummon == 召唤类型.风神) return new PAction(SMNSkill.风神召唤, ActionType.Gcd, ActionTargetType.Target);
        if (NextSummon == 召唤类型.火神) return new PAction(SMNSkill.火神召唤, ActionType.Gcd, ActionTargetType.Target);
        return null;
    }
}
