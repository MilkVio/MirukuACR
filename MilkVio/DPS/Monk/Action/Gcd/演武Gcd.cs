using Lumina.Excel.Sheets;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;

namespace MilkVio.DPS.Monk.Action.Gcd;

public class 演武Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        
        if (!PromeSettings.Instance.GetQt(MNKQt.自动演武)) return new CheckResult(false, "未开启自动演武");
        
        // 自身无需演武的条件
        if (Core.Me.HasStatus(MNKBuff.演武))
        {
            if (StatusHelper.GetStatusLeftTime(Core.Me, MNKBuff.演武) > 15)
            {
                return new CheckResult(false, "当前演武 > 15s 不需要演武");
            }
        }
        
        // 已自身有无目标来区分
        if (Core.Target == null)
        {
            if (TargetHelper.IsAllBossUntargetable())
            {
                return new CheckResult(true, "当前状态为所有Boss上天");
            }
        }
        else if (Core.Target != null)
        {
            if (JobGaugeHelper.MNK.Chakra == 5 && Core.Me.DistanceToMe() > currentMeleeRange)
            {
                return new CheckResult(true, "当前远离了boss");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(MNKSkill.演武, ActionType.Gcd, ActionTargetType.Self);
    }
}
