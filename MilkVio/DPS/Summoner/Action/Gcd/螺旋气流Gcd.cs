using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.Gcd;

public class 螺旋气流Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        // 自身不可以的条件
        if (MoveManager.IsLocalPlayerMoving && !me.HasStatus(SMNBuff.即刻咏唱)) return new CheckResult(false, "当前正在移动");
        
        // 决策不允许的条件
        if (PromeSettings.Instance.GetQt(SMNQt.龙神召唤))
        {
            if (SMNSkill.龙神召唤.GetActionCooldown() <= 1) return new CheckResult(false, "当前有龙神召唤");
        }

        if (me.HasStatus(SMNBuff.螺旋气流预备)) return new CheckResult(true, "有Buff打一个");
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SMNSkill.螺旋气流, ActionType.Gcd, ActionTargetType.Target);
    }
}
