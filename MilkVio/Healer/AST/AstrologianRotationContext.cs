using MilkVio.Healer.Common;
using PromeRotation.Data;

namespace MilkVio.Healer.AST;

public sealed class AstrologianRotationContext
{
    public ActionTargetType RaiseTarget { get; set; } = ActionTargetType.Self;

    public ActionTargetType SingleHealTarget { get; set; } = ActionTargetType.Self;

    public ActionTargetType PlayTarget { get; set; } = ActionTargetType.Self;

    public ActionTargetType CrownTarget { get; set; } = ActionTargetType.Self;

    public uint PlayAction { get; set; } = AstAction.出卡太阳;

    public uint CrownAction { get; set; } = AstAction.王冠之领主;

    public uint DrawAction { get; set; } = AstAction.星极抽卡;

    public uint SingleHealAction { get; set; } = AstAction.擢升;
}
