using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.Healer.AST.Openers;

public sealed class 占星无倒计时起手 : IOpener
{
    public const string OpenerDisplayName = "占星起手2：无倒计时";

    public string OpenerName => OpenerDisplayName;

    public List<PAction> InCombatSequence { get; } = AstOpenerHelper.Standard();

    public void InitializeCountdown(CountDownHandler countdownHandler)
        => AstOpenerHelper.InitializeStandardCountdown(countdownHandler, false);
}
