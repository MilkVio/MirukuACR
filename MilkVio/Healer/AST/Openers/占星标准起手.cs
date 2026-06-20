using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.Healer.AST.Openers;

public sealed class 占星标准起手 : IOpener
{
    public const string OpenerDisplayName = "占星起手1：标准100";

    public string OpenerName => OpenerDisplayName;

    public List<PAction> InCombatSequence { get; } = AstOpenerHelper.Standard();

    public void InitializeCountdown(CountDownHandler countdownHandler)
        => AstOpenerHelper.InitializeStandardCountdown(countdownHandler, true);
}
