using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.Healer.SGE.Openers;

public sealed class 均衡魂灵风息起手 : IOpener
{
    public const string OpenerDisplayName = "贤者起手1：均衡-魂灵风息";

    public string OpenerName => OpenerDisplayName;

    public List<PAction> InCombatSequence { get; } =
    [
        new(SgeAction.注药, ActionType.Gcd, ActionTargetType.Target),
        new(SgeAction.注药, ActionType.Gcd, ActionTargetType.Target),
        new(SgeAction.注药, ActionType.Gcd, ActionTargetType.Target),
    ];

    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        countdownHandler.AddAction(5000, new PAction(SgeAction.均衡, ActionType.Gcd, ActionTargetType.Self));
        countdownHandler.AddAction(1500, new PAction(SgeAction.魂灵风息, ActionType.Gcd, ActionTargetType.Target));
    }
}
