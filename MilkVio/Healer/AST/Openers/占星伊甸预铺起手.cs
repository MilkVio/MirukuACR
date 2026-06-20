using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.Healer.AST.Openers;

public sealed class 占星伊甸预铺起手 : IOpener
{
    public const string OpenerDisplayName = "占星起手3：伊甸预铺";

    public string OpenerName => OpenerDisplayName;

    public List<PAction> InCombatSequence { get; } = AstOpenerHelper.Standard();

    public void InitializeCountdown(CountDownHandler countdownHandler)
        => AstOpenerHelper.InitializeEdenCountdown(countdownHandler);
}
