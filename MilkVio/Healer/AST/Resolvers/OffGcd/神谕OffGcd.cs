using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.OffGcd;

public sealed class 神谕OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 神谕是占卜后的派生伤害，因此沿用“占卜”QT 作为开关。
        if (!HealerUtils.Qt(HealerQt.占卜)) return HealerUtils.Fail("占卜QT关闭");

        // 神谕是 25y 敌对目标伤害技能，只能在 Divining/神谕预备状态下执行。
        var targetCheck = HealerUtils.RequireEnemyTarget(25f);
        if (!targetCheck.Success) return targetCheck;

        // 只有身上有神谕预备 Buff 才能打；这个 Buff 通常由占卜产生。
        if (HealerUtils.Me is not { } me || !me.HasStatus(AstBuff.神谕预备)) return HealerUtils.Fail("没有神谕预备");

        // PAction 入队后不会在执行瞬间再 Adjust，所以这里直接使用神谕动作 ID。
        return HealerUtils.IsReady(AstAction.神谕, 92) ? HealerUtils.Pass("神谕") : HealerUtils.Fail("神谕不可用");
    }

    public PAction GetAction() => HealerUtils.OffGcd(AstAction.神谕, ActionTargetType.Target);
}
