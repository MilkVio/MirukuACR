using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Action.Gcd;

public class 强化三连Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Me.Level < 76) return new CheckResult(false, "当前等级不足");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentMeleeRange) return new CheckResult(false, "当前目标距离过远");
        var hasFof = Core.Me.HasStatus(PLDBuff.战逃反应buff);
        var fofCd = PLDSkill.战逃反应.GetActionCooldown();
        var me = Core.Me;
        
        if (Core.Me.DistanceToMe() <= currentMeleeRange)
        {
            if (Core.Me.HasStatus(PLDBuff.赎罪剑预备) || Core.Me.HasStatus(PLDBuff.祈告剑预备) || Core.Me.HasStatus(PLDBuff.葬送剑预备))
            {
                if (PromeSettings.Instance.GetQt(PLDQt.最优战逃))
                {
                    if (fofCd <= 3 && me.HasStatus(PLDBuff.神圣魔法效果提高) && me.HasStatus(PLDBuff.赎罪剑预备))
                    {
                        return new CheckResult(true, "最优战逃");
                    }
                }
                
                if (PromeSettings.Instance.GetQt(PLDQt.倾泻资源) || hasFof)
                {
                    return new CheckResult(true, "距离 < 3 开启倾泻资源");
                }
                
                if (!PromeSettings.Instance.GetQt(PLDQt.倾泻资源))
                {
                    if (ActionHelper.GetLastComboID() == PLDSkill.暴乱剑) return new CheckResult(true, "距离 < 3");
                }
            }
            return new CheckResult(false, "距离 < 3 但无Buff");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return PaladinHelper.GetBoostBaseGcd();
    }
}
