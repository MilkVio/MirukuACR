using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.Healer.SGE.Openers;

public sealed class 均衡箭毒起手 : IOpener
{
    public const string OpenerDisplayName = "贤者起手3：均衡-箭毒";

    public string OpenerName => OpenerDisplayName;

    public List<PAction> InCombatSequence { get; } =
    [
        new(SgeAction.箭毒, ActionType.Gcd, ActionTargetType.Target),
        new(SgeAction.注药, ActionType.Gcd, ActionTargetType.Target),
        new(SgeAction.注药, ActionType.Gcd, ActionTargetType.Target),
        new(SgeAction.注药, ActionType.Gcd, ActionTargetType.Target),
    ];

    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        countdownHandler.AddAction(5000, new PAction(SgeAction.均衡, ActionType.Gcd, ActionTargetType.Self));
    }
}
