using MilkVio.DPS.Samurai.Timeline;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Samurai;

public sealed class SamuraiJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        SamuraiPotionAction.Register(context);
        SamuraiMeikyoAction.Register(context);
        SamuraiForceMeikyoAction.Register(context);
        SamuraiGaugeCondition.Register(context);
    }
    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        => [("闪数量", "雪、月、花的持有数量，0～3", () => new SamuraiGaugeCondition(false)),
            ("明镜层数", "明镜止水技能剩余可用充能，0～2", () => new SamuraiGaugeCondition(true))];
    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => [("最优爆发药", "百级单体：在允许时间内择时用药，不能理想覆盖时按当前最佳安排使用", () => new SamuraiPotionAction()),
            ("请求一次明镜", "百级单体：绕过明镜QT，主动准备并在正常穿插使用一次明镜；战斗结束或切区清除", () => new SamuraiMeikyoAction()),
            ("强制请求一次明镜", "百级单体：与普通请求共用一份，允许无正常穿插时卡GCD；仍不主动明镜打雪", () => new SamuraiForceMeikyoAction())];
}
