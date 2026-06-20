using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.Gcd;

public class 深红旋风Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(SMNQt.深红旋风)) return new CheckResult(false, "未开启QT");
        
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        // 自身不可以的条件
        // if (MoveManager.IsLocalPlayerMoving) return new CheckResult(false, "当前正在移动");
        //if (SMNSkill.深红旋风.GetActionCooldown() != 0) return new CheckResult(false, "未冷却");
        
        // 决策不允许的条件
        if (PromeSettings.Instance.GetQt(SMNQt.龙神召唤))
        {
            if (SMNSkill.龙神召唤.GetActionCooldown() <= 1) return new CheckResult(false, "当前有龙神召唤");
        }
        
        if (PromeSettings.Instance.GetQt(SMNQt.快打三神) && JobGaugeHelper.SMN.SummonTimerRemaining == 0)
        {
            if (JobGaugeHelper.SMN.IsTitanReady) return new CheckResult(false, "土神好了 不打");
            if (JobGaugeHelper.SMN.IsGarudaReady) return new CheckResult(false, "风神好了 不打");
        }

        if (me.HasStatus(SMNBuff.深红旋风预备))
        {
            // 优先读条
            if (PromeSettings.Instance.GetQt(SMNQt.火神优先读条) && JobGaugeHelper.SMN.AttunementCount != 0)
            {
                if (MoveManager.IsLocalPlayerMoving) return new CheckResult(true, "优先读条 有打一个");
                return new CheckResult(false, "火神优先读条");
            }
            
            return new CheckResult(true, "有Buff打一个");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SMNSkill.深红旋风, ActionType.Gcd, ActionTargetType.Target);
    }
}
