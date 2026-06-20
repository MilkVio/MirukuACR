using PromeRotation.Timeline.Core;

namespace MilkVio.Healer.AST.Timeline;

public sealed class AstrologianJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        AstrologianGaugeCondition.Register(context);
        AstrologianQtAction.Register(context);
        AstrologianSkillAction.Register(context);
    }

    public IReadOnlyList<(string DisplayName, string Description, Func<ICondition> Create)> GetConditionDescriptors()
        =>
        [
            ("占星卡牌检测", "检测当前抽卡极性、手牌或王冠卡。", () => new AstrologianGaugeCondition()),
        ];

    public IReadOnlyList<(string DisplayName, string Description, Func<IAction> Create)> GetActionDescriptors()
        =>
        [
            ("占星QT控制", "开启或关闭占星 ACR 的指定 QT 行为。", () => new AstrologianQtAction()),
            ("占星技能执行", "按时间轴入队或强制执行一个占星技能。", () => new AstrologianSkillAction()),
        ];
}
