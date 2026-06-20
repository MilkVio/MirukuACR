using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.Healer.AST.Openers;

public sealed class 占星无起手 : IOpener
{
    public const string OpenerDisplayName = "占星起手0：仅DOT";

    public string OpenerName => OpenerDisplayName;

    public List<PAction> InCombatSequence { get; } = AstOpenerHelper.DotOnly();

    public void InitializeCountdown(CountDownHandler countdownHandler)
        => AstOpenerHelper.InitializeStandardCountdown(countdownHandler, true);
}
