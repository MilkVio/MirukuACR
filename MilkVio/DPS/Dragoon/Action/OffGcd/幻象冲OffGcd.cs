using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon.Action.OffGcd;

public class 幻象冲OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentAttackRange = GameData.GetCurrentAttackRange(20);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        // QT控制
        if (!PromeSettings.Instance.GetQt(DRGQt.高跳)) return new CheckResult(false, "已开启不打高跳");
        
        if (Core.Me.HasStatus(DRGBuff.幻象冲预备) && Core.Me.DistanceToMe() < CurrentAttackRange)
        {
            if (ActionHelper.GetGcdRemain() < 1f)
            {
                return new CheckResult(false, "GCDR不足");
            }
            if (Core.Me.HasStatus(DRGBuff.猛枪))
            {
                return new CheckResult(true, "所有条件满足 && 冷却完毕");
            }

            if (PromeSettings.Instance.GetQt(DRGQt.最终爆发)) return new CheckResult(true, "所有条件满足 && 冷却完毕");
            
            // 这里做一个简易的判断
            // 当幻象冲可以打进下一个猛枪的时候 留着等着打
            if (StatusHelper.GetStatusLeftTime(Core.Me, DRGBuff.幻象冲预备) - DRGSkill.猛枪.GetActionCooldown() - 2 > 0) 
            {
                return new CheckResult(false, "猛枪内可以打一个幻象冲 等一下");
            }
            
            return new CheckResult(true, "所有条件满足 && 冷却完毕");
        }
        
        return new CheckResult(false, "未获得幻象冲预备Buff");
    }

    public PAction GetAction()
    {
        return new PAction(DRGSkill.幻象冲, ActionType.OffGcd, ActionTargetType.Target);
    }
}
