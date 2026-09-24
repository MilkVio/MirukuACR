using MilkVio.DPS.Machinist.Timeline;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Machinist;

public sealed class MachinistJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        MachinistOutputWindowAction.Register(context);
        MachinistClearWindowAction.Register(context);
    }
    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors() => [];
    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors() =>
    [
        ("设置可输出窗口", "预计结束与期末热量、电量；遵守QT", () => new MachinistOutputWindowAction()),
        ("清除可输出窗口", "解除窗口及期末资源要求", () => new MachinistClearWindowAction())
    ];
}
