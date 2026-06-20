using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.Healer.SGE.Openers;

public sealed class 注药均衡起手 : IOpener
{
    public const string OpenerDisplayName = "贤者起手2：注药-均衡";

    public string OpenerName => OpenerDisplayName;

    public List<PAction> InCombatSequence { get; } =
    [
        new(SgeAction.均衡, ActionType.Gcd, ActionTargetType.Self),
        new(SgeAction.注药, ActionType.Gcd, ActionTargetType.Target),
        new(SgeAction.注药, ActionType.Gcd, ActionTargetType.Target),
        new(SgeAction.注药, ActionType.Gcd, ActionTargetType.Target),
    ];

    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        countdownHandler.AddAction(1500, new PAction(SgeAction.注药, ActionType.Gcd, ActionTargetType.Target));
    }
}
