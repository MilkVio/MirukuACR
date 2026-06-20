using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.Gcd;

public sealed class 重力Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 只有 AOE QT 打开时才允许进入群体 GCD 逻辑。
        if (!HealerUtils.Qt(HealerQt.AOE)) return HealerUtils.Fail("AOE QT关闭");

        // 重力系 45 级才可用；等级不足时直接交给后面的 GCD resolver。
        if (HealerUtils.Level < 45) return HealerUtils.Fail("等级不足");

        // AOE 也需要一个合法敌对目标；false 表示移动中不允许硬读重力。
        var targetCheck = HealerUtils.RequireEnemyTarget(25f, 400, false);
        if (!targetCheck.Success) return targetCheck;

        // 当前目标 5m 内至少 3 个敌人才打重力，否则继续交给 DOT/凶星。
        return HealerUtils.EnemyCountAroundTarget(5f) >= 3 ? HealerUtils.Pass("重力AOE") : HealerUtils.Fail("AOE目标不足");
    }

    public PAction GetAction() => HealerUtils.Gcd(AstAction.重力, ActionTargetType.Target);
}
