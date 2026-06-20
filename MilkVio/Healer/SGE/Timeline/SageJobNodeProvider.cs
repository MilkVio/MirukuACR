using PromeRotation.Timeline.Core;

namespace MilkVio.Healer.SGE.Timeline;

public sealed class SageJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        SageGaugeCondition.Register(context);
        SageQtAction.Register(context);
        SageSkillAction.Register(context);
    }

    public IReadOnlyList<(string DisplayName, string Description, Func<ICondition> Create)> GetConditionDescriptors()
        =>
        [
            ("贤者资源检测", "检测蛇胆、蛇刺、均衡状态或蛇胆计时。", () => new SageGaugeCondition()),
        ];

    public IReadOnlyList<(string DisplayName, string Description, Func<IAction> Create)> GetActionDescriptors()
        =>
        [
            ("贤者QT控制", "开启或关闭贤者 ACR 的指定 QT 行为。", () => new SageQtAction()),
            ("贤者技能执行", "按时间轴入队或强制执行一个贤者技能。", () => new SageSkillAction()),
        ];
}
