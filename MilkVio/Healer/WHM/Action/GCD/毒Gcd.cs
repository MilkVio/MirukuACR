using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.Healer.WHM.WHMData;

namespace MilkVio.Healer.WHM.Action.GCD;

public class 毒Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        var target = Core.Target;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (me.CurrentMp < 400) return new CheckResult(false, "魔力不足");
        
        if (target == null) return new CheckResult(false, "当前无目标");
        if (target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");

        if (PromeSettings.Instance.GetQt(WHMQt.只打闪灼)) return new CheckResult(false, "当前只打闪灼");

        if (PromeSettings.Instance.GetQt(WHMQt.只打续毒)) return new CheckResult(true, "当前只打续毒");

        if (PromeSettings.Instance.GetQt(WHMQt.续毒移动) && MoveManager.IsLocalPlayerMoving)
        {
            return new CheckResult(true, "移动续毒");
        }
        
        var 毒Buff = WhiteMageHelper.GetCurrent毒StatusId();
        if (target.HasStatus(毒Buff))
        {
            if (target.GetStatusLeftTime(毒Buff) < 3)
            {
                return new CheckResult(true, "续毒");
            }
            return new CheckResult(false, "当前毒未过期");
        }
        
        return new CheckResult(true, "打一个");
    }

    public PAction GetAction()
    {
        return new PAction(WhiteMageHelper.GetCurrent毒ActionId(), ActionType.Gcd, ActionTargetType.Target);
    }
}
