using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.OffGcd;

public sealed class 王冠卡OffGcd : IDecisionResolver
{
    private readonly AstrologianRotationContext _context;

    public 王冠卡OffGcd(AstrologianRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        // 王冠卡分两条：Lord 是伤害，Lady 是治疗；当前注册顺序在抽卡前，避免抽卡覆盖未打出的 Lord。
        var crownCard = HealerUtils.AstDrawnCrownCard();

        if (crownCard == "Lord")
        {
            // Lord 由“剑卡”QT 控制，按伤害技能处理，并且只在占卜/120s 卡牌窗口内打。
            if (!HealerUtils.Qt(HealerQt.剑卡)) return HealerUtils.Fail("剑卡QT关闭");
            if (!HealerUtils.AstCardWindow()) return HealerUtils.Fail("未到占卜窗口");

            // 虽然 Lord 目标写 Self，但仍要求有合法敌对目标，避免无目标时交爆发资源。
            var targetCheck = HealerUtils.RequireEnemyTarget(25f);
            if (!targetCheck.Success) return targetCheck;

            _context.CrownAction = AstAction.王冠之领主;
            _context.CrownTarget = ActionTargetType.Self;
            return HealerUtils.IsReady(AstAction.王冠之领主, 70) ? HealerUtils.Pass("王冠之领主") : HealerUtils.Fail("领主不可用");
        }

        if (crownCard == "Lady" && HealerUtils.BattleTimeAtLeast(10) && HealerUtils.LowPartyCount(99f) >= 1)
        {
            // Lady 由“冠卡”QT 控制；默认关闭。当前逻辑只要战斗 10s 后有人不满血就允许打。
            if (!HealerUtils.Qt(HealerQt.冠卡)) return HealerUtils.Fail("冠卡QT关闭");
            _context.CrownAction = AstAction.王冠之贵妇;
            _context.CrownTarget = ActionTargetType.Self;
            return HealerUtils.IsReady(AstAction.王冠之贵妇, 70) ? HealerUtils.Pass("王冠之贵妇") : HealerUtils.Fail("贵妇不可用");
        }

        return HealerUtils.Fail("没有可用冠卡");
    }

    public PAction GetAction() => HealerUtils.OffGcd(_context.CrownAction, _context.CrownTarget);
}
