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
        SamuraiClearAction.Register(context);
        SamuraiMeditationAction.Register(context);
        SamuraiHiganbanaBlacklistAction.Register(context);
        SamuraiPotionCooldownCondition.Register(context);
    }
    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        => [("闪数量", "雪、月、花的持有数量，0～3", () => new SamuraiGaugeCondition(false)),
            ("明镜层数", "明镜止水技能剩余可用充能，0～2", () => new SamuraiGaugeCondition(true)),
            ("爆发药冷却", "当前携带爆发药的剩余冷却秒数", () => new SamuraiPotionCooldownCondition())];
    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => [("最优爆发药", "在允许时间内择时用药", () => new SamuraiPotionAction()),
            ("请求一次明镜", "绕过明镜QT，正常穿插一次", () => new SamuraiMeikyoAction()),
            ("强制请求一次明镜", "允许卡GCD，仍不主动明镜打雪", () => new SamuraiForceMeikyoAction()),
            ("清除当前明镜请求", "不撤回已入队技能", () => new SamuraiClearAction()),
            ("默想", "满足条件后默想，期间暂停自动求解", () => new SamuraiMeditationAction()),
            ("清理默想状态", "清除等待与保护，恢复求解", () => new SamuraiClearAction(true)),
            ("添加彼岸花黑名单", "按BaseId禁止自动上花和续花", () => new SamuraiHiganbanaBlacklistAction()),
            ("清除彼岸花黑名单", "清除全部BaseId", () => new SamuraiHiganbanaBlacklistAction(true))];
}
