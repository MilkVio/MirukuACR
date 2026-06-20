using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.OffGcd;

public sealed class 光速OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        // 光速 QT 是总开关；关闭时不自动交光速。
        if (!HealerUtils.Qt(HealerQt.光速)) return HealerUtils.Fail("光速QT关闭");

        // 开场 10s 内交给 opener 队列，常规 120s 循环从 10s 后开始接管。
        if (!HealerUtils.BattleTimeAtLeast(10)) return HealerUtils.Fail("开场10秒内交给起手");

        if (HealerUtils.Me is not { } me) return HealerUtils.Fail("自身未加载");
        if (me.HasStatus(AstBuff.光速)) return HealerUtils.Fail("已有光速");

        // 只要有至少 1 层光速就允许进入后续窗口判断。
        if (!HealerUtils.HasCharge(AstAction.光速, 6)) return HealerUtils.Fail("光速无层数");

        // 当前逻辑：占卜剩余冷却 <= 5s 时预交光速，等价于把光速绑定到 120s 爆发前。
        var divinationCooldown = HealerUtils.ActionCooldown(AstAction.占卜, 50);
        return divinationCooldown <= 5f
            ? HealerUtils.Pass("占卜前光速")
            : HealerUtils.Fail("占卜冷却还早");
    }

    public PAction GetAction() => HealerUtils.OffGcd(AstAction.光速, ActionTargetType.Self);
}
