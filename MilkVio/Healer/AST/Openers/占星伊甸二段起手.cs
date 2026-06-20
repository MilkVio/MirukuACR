using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.Healer.AST.Openers;

public sealed class 占星伊甸二段起手 : IOpener
{
    public const string OpenerDisplayName = "占星起手4：伊甸二段";

    public string OpenerName => OpenerDisplayName;

    public List<PAction> InCombatSequence { get; } = AstOpenerHelper.EdenTwo();

    public void InitializeCountdown(CountDownHandler countdownHandler)
        => AstOpenerHelper.InitializeStandardCountdown(countdownHandler, true);
}
