using Lumina.Excel.Sheets.Experimental;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai.Action.OffGcd;

/*
 * 这里需要写一个优先级：
 * 如果有残心buff 剑气 > 85再使用
 * 如果有必杀剑120那个 cd < 5 或者好了 这个也变为50泄
 * 但是实际上这里属于120或者团辅 应该上述为优先级最高的模式
 */
public class 必杀剑_震天OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        var kenki = JobGaugeHelper.SAM.剑气;
        var isCanUse = SAMSkill.必杀剑_震天.GetActionCooldown() == 0 && kenki >= 25;
        var 必杀剑cd = SAMSkill.必杀剑_闪影.GetActionCooldown();
        var 意气冲天cd = SAMSkill.意气冲天.GetActionCooldown();
        var me = Core.Me;
        
        if (isCanUse)
        {
            if (me.HasStatus(SAMBuff.残心预备))
            {
                if (kenki >= 75)
                {
                    return new CheckResult(true, $"残心泄");
                }
                return new CheckResult(false, "先打残心");
            }
            
            if (意气冲天cd < 5 && kenki >= 50)
            {
                return new CheckResult(true, $"120前泄");
            }
            
            if (必杀剑cd < 5)
            {
                if (kenki >= 75)
                {
                    return new CheckResult(true, $"必杀剑泄");
                }
                return new CheckResult(false, "先打必杀剑");
            }

            if (GameData.IsIn120() || SamuraiHelper.IsInSelf120() || PromeSettings.Instance.GetQt(SAMQt.倾泻资源))
            {
                return new CheckResult(true, $"120中泄");
            }
            
            if (kenki >= 75)
            {
                return new CheckResult(true, $"正常泄");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SAMSkill.必杀剑_震天, ActionType.OffGcd, ActionTargetType.Target);
    }
}
