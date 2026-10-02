using PromeRotation.Data;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.Level100;

namespace MilkVio.DPS.Samurai.Action.Gcd;

public class 居合等待Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Me == null || Core.Target == null || Core.Target.IsDead || !Core.Target.IsTargetable ||
            Core.Target.EntityId == Core.Me.EntityId || !Samurai100Helper.Enabled)
            return new CheckResult(false, "当前不接管居合等待");
        var s = Samurai100Planning.ReadState();
        if (s.ForcePadding && s.Distance > s.MeleeRange && (Samurai100Rules.HoldTsubame(s) || Samurai100Rules.PaddingGcd(s, out _)))
            return new CheckResult(true, "强制垫刀，等待回到连击距离");
        if (Samurai100Helper.WaitingIaijutsu() == 0) return new CheckResult(false, "无需保留连击等待");
        return new CheckResult(true, Samurai100Rules.WaitReason(Samurai100Planning.ReadState()));
    }

    // 宿主允许空动作等待，不再落到晓风或燕飞。
    public PAction GetAction() => null!;
}
