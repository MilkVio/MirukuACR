using ECommons.DalamudServices;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;

namespace MilkVio.DPS.Dancer.Action.Gcd;

public class 基础AOEGcd : IDecisionResolver //12 强化12 大舞小舞结束动作落幕舞
{
    public CheckResult Check()
    {
        var player = Core.Me;
        if (player == null) return new CheckResult(false, "我不存在");
        
        if (!PromeSettings.Instance.GetQt(DNCQt.启用多目标)) return new CheckResult(false, "未开启AOE");
        
        if (TargetHelper.EnemyInRange(5) >= 2)
        {
            return new CheckResult(true, "敌人数量足够");
        }
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var me = Core.Me;
        
        //强化1
        if (me.HasStatus(DNCBuff.对称投掷) || me.HasStatus(DNCBuff.百花对称投掷) == true)
        {
            return new PAction(DNCSkill.升风车, ActionType.Gcd, ActionTargetType.Self);
        }
        //强化2
        if (me.HasStatus(DNCBuff.非对称投掷) || me.HasStatus(DNCBuff.百花非对称投掷) == true)
        {
            return new PAction(DNCSkill.落血雨, ActionType.Gcd, ActionTargetType.Self);
        }
        
        
        //12
        if (ActionHelper.GetLastComboID() == DNCSkill.风车)
        {
            return new PAction(DNCSkill.落刃雨, ActionType.Gcd, ActionTargetType.Self);
        }

        return new PAction(DNCSkill.风车, ActionType.Gcd, ActionTargetType.Self);
    }
}
