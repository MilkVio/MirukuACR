using MilkVio.DPS.Samurai.Timeline;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Samurai;

public sealed class SamuraiJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context) => SamuraiPotionAction.Register(context);
    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        => Array.Empty<(string, string, Func<ICondition>)>();
    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => [("最优爆发药", "百级单体：在允许时间内择时用药，不能理想覆盖时按当前最佳安排使用", () => new SamuraiPotionAction())];
}
