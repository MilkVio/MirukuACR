using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.AST.Resolvers.OffGcd;

public sealed class 抽卡OffGcd : IDecisionResolver
{
    private readonly AstrologianRotationContext _context;

    public 抽卡OffGcd(AstrologianRotationContext context)
    {
        _context = context;
    }

    public CheckResult Check()
    {
        // 抽卡 QT 是自动星极/灵极抽卡开关。
        if (!HealerUtils.Qt(HealerQt.抽卡)) return HealerUtils.Fail("抽卡QT关闭");

        // 开场 10s 内只有已经进入占卜/神谕窗口才抽卡；10s 后交给常规 120s 循环。
        if (!HealerUtils.BattleTimeAtLeast(10) && !HealerUtils.AstHasDivinationBuffWindow())
            return HealerUtils.Fail("开场未到抽卡窗口");

        // 已经有太阳/月亮伤害卡时不抽，防止覆盖还没发出的卡。
        if (HealerUtils.HasAstDamageCard()) return HealerUtils.Fail("已有太阳/月亮卡");

        // Lord（剑卡）未打出时不抽下一套，保证王冠伤害先进爆发窗口。
        if (HealerUtils.HasAstLordCard()) return HealerUtils.Fail("已有剑卡");

        // ActiveDraw 是 Prome 量谱暴露的当前可抽极性：Astral 对应星极，Umbral 对应灵极。
        if (!HealerUtils.TryAstActiveDraw(out var activeDraw)) return HealerUtils.Fail("Prome占星量谱未加载");

        if (activeDraw != "Astral" && activeDraw != "Umbral") return HealerUtils.Fail($"未知抽卡极性:{activeDraw}");

        // 根据当前极性选择具体动作，GetAction() 会把这个动作打出去。
        _context.DrawAction = activeDraw == "Astral" ? AstAction.星极抽卡 : AstAction.灵极抽卡;
        return HealerUtils.IsReady(_context.DrawAction, 30) ? HealerUtils.Pass("抽卡") : HealerUtils.Fail("抽卡未冷却");
    }

    public PAction GetAction() => HealerUtils.OffGcd(_context.DrawAction, ActionTargetType.Self);
}
