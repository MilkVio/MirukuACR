using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;
using MilkVio.DPS.RedMage.RDMData;


namespace MilkVio.DPS.RedMage.Action.Gcd;

public class 魔回刺Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(3);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.HasStatus(RDMBuff.魔元化Buff)) currentAttackRange = GameData.GetCurrentAttackRange(25);
        
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        // 自身不可以的条件
        //if (RedMageHelper.HasContinuousCast(me)) return new CheckResult(false, "当前有连续咏唱");
        if (!RedMageHelper.IsInManaActionCombo(me) && RedMageHelper.CanUseMana3Action(me))
        {
            if (PromeSettings.Instance.GetQt(RDMQt.倾泻资源))
            {
                return new CheckResult(true, "倾泻资源");
            }
            else
            {
                if (RedMageHelper.HasContinuousCast(me)) return new CheckResult(false, "当前有连续咏唱");
                var cd120 = RDMSkill.鼓励.GetActionCooldown();
                var isCan120Use = cd120 < 40 && cd120 > 4;
                
                if (!PromeSettings.Instance.GetQt(RDMQt.不打120))
                {
                    if (isCan120Use) return new CheckResult(false, "等120一起爆发");
                }
                
                //这里再加一个 未开启不打120之前30秒保留一些资源
                return new CheckResult(true, "正常满50打");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(RDMSkill.魔回刺1, ActionType.Gcd, ActionTargetType.Target);
    }
}
