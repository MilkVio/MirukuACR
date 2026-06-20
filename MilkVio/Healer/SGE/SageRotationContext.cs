using PromeRotation.Data;

namespace MilkVio.Healer.SGE;

public sealed class SageRotationContext
{
    public ActionTargetType RaiseTarget { get; set; } = ActionTargetType.Self;

    public ActionTargetType SingleHealTarget { get; set; } = ActionTargetType.Self;

    public ActionTargetType KardiaTarget { get; set; } = ActionTargetType.Self;

    public ActionTargetType CleanseTarget { get; set; } = ActionTargetType.Self;
}
