using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.Healer.SCH.Openers;

public sealed class 学者Ultimate起手 : IOpener
{
    public const string OpenerDisplayName = "学者起手：Ultimate";

    public string OpenerName => OpenerDisplayName;

    public List<PAction> InCombatSequence { get; } = ScholarOpenerHelper.Ultimate();

    public void InitializeCountdown(CountDownHandler countdownHandler)
        => ScholarOpenerHelper.InitializeCountdown(countdownHandler);
}
