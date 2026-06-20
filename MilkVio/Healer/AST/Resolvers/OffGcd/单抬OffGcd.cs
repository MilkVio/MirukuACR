using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.OffGcd;

public sealed class 单抬OffGcd : IDecisionResolver
{
    private readonly AstrologianRotationContext _context;

    public 单抬OffGcd(AstrologianRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        if (!HealerUtils.Qt(HealerQt.单抬)) return HealerUtils.Fail("单抬QT关闭");
        if (!HealerUtils.TryGetPartyBelowAverage(90f, 10f, out var target)) return HealerUtils.Fail("没有明显低血目标");

        _context.SingleHealTarget = target;

        if (HealerUtils.HasCharge(AstAction.擢升, 15))
        {
            _context.SingleHealAction = AstAction.擢升;
            return HealerUtils.Pass("单抬：擢升");
        }

        if (HealerUtils.HasCharge(AstAction.天星交错, 74))
        {
            _context.SingleHealAction = AstAction.天星交错;
            return HealerUtils.Pass("单抬：天星交错");
        }

        if (!HealerUtils.TryAstActiveDraw(out var activeDraw))
            return HealerUtils.Fail("Prome占星量谱未加载");

        if (activeDraw == "Astral")
        {
            if (HealerUtils.IsReady(AstAction.出卡世界树, 30))
            {
                _context.SingleHealAction = AstAction.出卡世界树;
                return HealerUtils.Pass("单抬：世界树卡");
            }

            if (HealerUtils.IsReady(AstAction.出卡河流, 30))
            {
                _context.SingleHealAction = AstAction.出卡河流;
                return HealerUtils.Pass("单抬：河流卡");
            }
        }

        if (activeDraw == "Umbral")
        {
            if (HealerUtils.IsReady(AstAction.出卡塔, 30))
            {
                _context.SingleHealAction = AstAction.出卡塔;
                return HealerUtils.Pass("单抬：塔卡");
            }

            if (HealerUtils.IsReady(AstAction.出卡箭, 30))
            {
                _context.SingleHealAction = AstAction.出卡箭;
                return HealerUtils.Pass("单抬：箭卡");
            }
        }

        return HealerUtils.Fail("没有可用单抬能力");
    }

    public PAction GetAction() => HealerUtils.OffGcd(_context.SingleHealAction, _context.SingleHealTarget);
}
