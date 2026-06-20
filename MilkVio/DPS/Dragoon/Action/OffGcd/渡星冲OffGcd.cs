using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon.Action.OffGcd;

public class 渡星冲OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentAttackRange = GameData.GetCurrentAttackRange(20);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.Level < 100) return new CheckResult(false, "等级不足");
        
        // QT控制
        if (PromeSettings.Instance.GetQt(DRGQt.不打60)) return new CheckResult(false, "已开启不打120");
        
        if (Core.Me.HasStatus(DRGBuff.渡星冲预备) && Core.Me.DistanceToMe() < CurrentAttackRange)
        {
            return new CheckResult(true, "所有条件满足 && 冷却完毕");
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(DRGSkill.渡星冲, ActionType.OffGcd, ActionTargetType.Target);
    }
}
