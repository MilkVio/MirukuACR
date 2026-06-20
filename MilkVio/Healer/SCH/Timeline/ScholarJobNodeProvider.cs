using PromeRotation.Timeline.Core;

namespace MilkVio.Healer.SCH.Timeline;

public sealed class ScholarJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        ScholarGaugeCondition.Register(context);
        ScholarQtAction.Register(context);
        ScholarSkillAction.Register(context);
        ScholarAetherflowReserveAction.Register(context);
    }

    public IReadOnlyList<(string DisplayName, string Description, Func<ICondition> Create)> GetConditionDescriptors()
        =>
        [
            ("学者资源检测", "检测以太超流、异想以太、炽天使时间或朝日状态。", () => new ScholarGaugeCondition()),
        ];

    public IReadOnlyList<(string DisplayName, string Description, Func<IAction> Create)> GetActionDescriptors()
        =>
        [
            ("学者QT控制", "开启或关闭学者 ACR 的指定 QT 行为。", () => new ScholarQtAction()),
            ("学者技能执行", "按时间轴入队或强制执行一个学者技能。", () => new ScholarSkillAction()),
            ("学者豆子保留", "设置能量吸收需要保留的以太超流层数。", () => new ScholarAetherflowReserveAction()),
        ];
}
