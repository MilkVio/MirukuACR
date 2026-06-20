using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.Gcd;

public sealed class 焚灼Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 续 DOT 前先保证当前目标能打、蓝量至少 400；这里允许移动，因为移动时可用 DOT 滑步。
        var targetCheck = HealerUtils.RequireEnemyTarget(25f, 400);
        if (!targetCheck.Success) return targetCheck;

        // 没有瞬发状态且角色正在移动时，只在“滑步续毒”模式下允许提前补 DOT。
        var movingWithoutInstant = HealerUtils.IsMovingWithoutInstant();
        if (movingWithoutInstant && MioAcrSettings.Instance.AstSlideMode != 0)
            return HealerUtils.Fail("移动中且未选择滑步续毒");

        // 静止时只在 DOT 约 1 个 GCD 内到期才续；移动滑步时放宽到 10s，避免空转。
        var refreshAt = movingWithoutInstant ? 10f : 2.7f;

        // CheckAstDot 会区分“目标无 DOT”“DOT 即将到期”和各种不补 DOT 的原因，避免无 DOT 时误报时间充足。
        var dotCheck = HealerUtils.CheckAstDot(refreshAt, AstBuff.焚灼, AstBuff.焚灼II, AstBuff.焚灼III);
        return dotCheck.Success
            ? HealerUtils.Pass(movingWithoutInstant ? $"滑步续毒:{dotCheck.Message}" : $"续毒:{dotCheck.Message}")
            : dotCheck;
    }

    public PAction GetAction() => HealerUtils.Gcd(AstAction.焚灼, ActionTargetType.Target);
}
