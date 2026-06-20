using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon.Action.OffGcd;

public class 天龙点睛OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var CurrentAttackRange = GameData.GetCurrentAttackRange(15);
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Me.Level < 90) return new CheckResult(false, "等级不足");
        
        // QT控制
        if (!PromeSettings.Instance.GetQt(DRGQt.天龙点睛)) return new CheckResult(false, "已关闭天龙点睛");

        if (JobGaugeHelper.DRG.FirstmindsFocusCount == 2 && Core.Me.DistanceToMe() < CurrentAttackRange)
        {
            // 里面的逻辑
            // todo 60秒或者120秒就直接打掉
            // 否则延后到云蒸龙变之后打掉 √
            
            // 120秒
            if (Core.Me.HasStatus(DRGBuff.战斗连祷) && Core.Me.HasStatus(DRGBuff.猛枪))
            {
                return new CheckResult(true, "120中 好了就打");
            }

            if (Core.Me.HasStatus(DRGBuff.战斗连祷) && StatusHelper.GetStatusLeftTime(Core.Me, DRGBuff.战斗连祷) < 10)
            {
                return new CheckResult(true, "120中 好了就打");
            }
            
            // 60秒
            if (Core.Me.HasStatus(DRGBuff.猛枪) && DRGSkill.战斗连祷.GetActionCooldown() > 30)
            {
                return new CheckResult(true, "60中 好了就打");
            }
            
            if (ActionHelper.GetLastComboID() == DRGSkill.云蒸龙变)
            {
                return new CheckResult(true, "上一个是云蒸龙变 && 冷却完毕");
            }
            return new CheckResult(false, "可以使用 但不满足任何条件");
        }
        // return new CheckResult(true, "所有条件满足 && 冷却完毕");
        return new CheckResult(false, "龙眼不足");
    }

    public PAction GetAction()
    {
        return new PAction(DRGSkill.天龙点睛, ActionType.OffGcd, ActionTargetType.Target);
    }
}
