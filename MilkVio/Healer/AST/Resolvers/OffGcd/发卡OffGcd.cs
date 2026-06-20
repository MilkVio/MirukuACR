using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.OffGcd;

public sealed class 发卡OffGcd : IDecisionResolver
{
    private readonly AstrologianRotationContext _context;

    public 发卡OffGcd(AstrologianRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        // 发卡 QT 控制太阳/月亮两张伤害卡；治疗卡不会从这里打出。
        if (!HealerUtils.Qt(HealerQt.发卡)) return HealerUtils.Fail("发卡QT关闭");

        // 发卡只认自己身上的占卜 Buff；不再用占卜 CD 预测窗口，避免提前或延后乱发。
        if (!HealerUtils.HasAstDivinationBuff()) return HealerUtils.Fail("没有占卜Buff");

        // 只识别 Balance/Spear 两张伤害卡；从整组手牌里找，避免第一格为空时漏掉太阳/月亮。
        var card = HealerUtils.AstDrawnDamageCard();
        if (!HealerUtils.IsAstDamageCard(card)) return HealerUtils.Fail("没有伤害卡");

        if (card == "Balance")
        {
            // Balance 对应太阳卡，目标使用 BestAstCardTarget 的近战优先级。
            _context.PlayAction = AstAction.出卡太阳;
            _context.PlayTarget = HealerUtils.BestAstCardTarget(card);
            return HealerUtils.IsReady(AstAction.出卡太阳, 30) ? HealerUtils.Pass("太阳卡") : HealerUtils.Fail("太阳卡不可用");
        }

        if (card == "Spear")
        {
            // Spear 对应月亮卡，目标使用 BestAstCardTarget 的远程优先级。
            _context.PlayAction = AstAction.出卡月亮;
            _context.PlayTarget = HealerUtils.BestAstCardTarget(card);
            return HealerUtils.IsReady(AstAction.出卡月亮, 30) ? HealerUtils.Pass("月亮卡") : HealerUtils.Fail("月亮卡不可用");
        }

        return HealerUtils.Fail("没有伤害卡");
    }

    public PAction GetAction() => HealerUtils.OffGcd(_context.PlayAction, _context.PlayTarget);
}
