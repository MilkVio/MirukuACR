using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja.Action.Gcd;

public class 残影镰鼬Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(20);
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        var isCanUse = Core.Me.HasStatus(NinjaBuff.残影镰鼬预备);
        if (!isCanUse) return new CheckResult(false, "当前没有预备Buff");
        
        // 理论上这里最好可以打到60/120中
        var statusLeftTime = StatusHelper.GetStatusLeftTime(Core.Me, NinjaBuff.残影镰鼬预备);
        
        if (isCanUse)
        {
            if (Core.Me.DistanceToMe() > currentMeleeRange)
            {
                return new CheckResult(true, "远离镰鼬");
            }
            
            bool use120 = !PromeSettings.Instance.GetQt(NinjaQt.不打120) || PromeSettings.Instance.GetQt(NinjaQt.镰鼬对齐120);
            bool use60 = !PromeSettings.Instance.GetQt(NinjaQt.不打60) && !PromeSettings.Instance.GetQt(NinjaQt.镰鼬对齐120);

            // 如果正在爆发窗口中，直接打掉
            if (use120 && NinjaHelper.Is120())
                return new CheckResult(true, "120打掉");
            if (use60 && NinjaHelper.Is60())
                return new CheckResult(true, "60打掉");

            // 两个窗口都不参与 → 好了就用
            if (!use120 && !use60)
                return new CheckResult(true, "好了就用");

            // 选择最近的爆发窗口
            float bestCd = float.MaxValue;
            float waitThreshold = 0f;
            string bestName = "";

            if (use120)
            {
                float cd = NinjaSkill.介毒之术.GetActionCooldown();
                if (cd < bestCd) { bestCd = cd; waitThreshold = cd + 2.5f; bestName = "120"; }
            }

            if (use60)
            {
                float cd = NinjaHelper.GetNinja60ActionId().GetActionCooldown();
                if (cd < bestCd) { bestCd = cd; waitThreshold = cd + 2.5f; bestName = "60"; }
            }

            if (bestCd < float.MaxValue)
            {
                if (statusLeftTime > waitThreshold)
                    return new CheckResult(false, $"等待{bestName}");

                if (statusLeftTime < bestCd)
                    return new CheckResult(true, $"{bestName}打掉");
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(NinjaSkill.残影镰鼬, ActionType.Gcd, ActionTargetType.Target);
    }
}
