using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.Gcd;

public sealed class 凶星Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 凶星是单体 GCD 兜底：前面的 AOE 和续 DOT 都不通过时才会走到这里。
        // false 表示移动且没有瞬发时不硬读，避免在移动中反复尝试读条。
        var targetCheck = HealerUtils.RequireEnemyTarget(25f, 400, false);
        if (!targetCheck.Success) return targetCheck;

        // 只要目标合法并且能读条，就交给 GetAction() 打当前等级对应的凶星系技能。
        return HealerUtils.Pass("基础填充");
    }

    public PAction GetAction() => HealerUtils.Gcd(AstAction.凶星, ActionTargetType.Target);
}
