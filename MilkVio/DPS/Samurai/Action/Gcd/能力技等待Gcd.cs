using MilkVio.DPS.Samurai.Level100;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.DPS.Samurai.Action.Gcd;

public class 能力技等待Gcd : IDecisionResolver
{
    public CheckResult Check() => Samurai100Weave.TryEmergency(out _, out var reason, true)
        ? new CheckResult(true, reason) : new CheckResult(false, "无需紧急能力技");

    public PAction GetAction() => null!;
}
