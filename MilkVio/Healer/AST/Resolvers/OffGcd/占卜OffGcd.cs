using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.OffGcd;

public sealed class 占卜OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 占卜 QT 是 120s 爆发轴总开关之一；关闭时不自动交占卜。
        if (!HealerUtils.Qt(HealerQt.占卜)) return HealerUtils.Fail("占卜QT关闭");

        // 开场 10s 内交给 opener 队列，避免常规 resolver 抢起手里的占卜。
        if (!HealerUtils.BattleTimeAtLeast(10)) return HealerUtils.Fail("开场10秒内交给起手");

        // 占卜本身是自身增益，但这里仍要求当前有合法敌对目标，避免脱战/无目标乱开。
        var targetCheck = HealerUtils.RequireEnemyTarget(25f);
        if (!targetCheck.Success) return targetCheck;

        // CD 转好就打；真正的“120s 对齐”目前主要靠占卜自身冷却自然形成。
        return HealerUtils.IsReady(AstAction.占卜, 50) ? HealerUtils.Pass("占卜") : HealerUtils.Fail("占卜未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(AstAction.占卜, ActionTargetType.Self);
}
