using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Samurai.SAMData;
using MilkVio.DPS.Samurai.Level100;

namespace MilkVio.DPS.Samurai.Action.OffGcd;

public class 真北OffGcd : IDecisionResolver
{
    private readonly Func<PAction?> _nextGcd;

    public 真北OffGcd(Func<PAction?> nextGcd)
    {
        _nextGcd = nextGcd;
    }

    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (!PromeSettings.Instance.GetQt(SAMQt.真北)) return new CheckResult(false, "未开启自动真北");
        if (!TargetHelper.HasPositionalRequirement(Core.Target)) return new CheckResult(false, "目标无需身位");
        if (Core.Me.IsCasting) return new CheckResult(false, "当前正在读条");
        if (Samurai100Helper.Enabled && float.IsFinite(Samurai100Weave.CooldownDelay(Samurai100Planning.ReadState())))
            return new CheckResult(false, "给即将转好的意气或闪影留位置");
        if (UniversalData.MeleeUniversalSkill.真北.GetActionCharges() < 1) return new CheckResult(false, "真北没有充能");

        var action = _nextGcd()?.ActionId ?? 0;
        var need = SamuraiHelper.GetNeedPositional(action);
        if (need == Positional.None) return new CheckResult(false, "下一刀不需要身位");
        if (TargetHelper.GetTargetPositional() == need) return new CheckResult(false, "当前已在所需身位");
        var gcdLeft = ActionHelper.GetGcdRemain();
        if (Core.Me.GetStatusLeftTime(1250) > gcdLeft + Samurai100Helper.EffectMargin)
            return new CheckResult(false, "现有真北可覆盖下一刀");
        if (gcdLeft < Math.Max(0, ActionHelper.GetAnimationLock()) + Samurai100Projection.AbilityLock)
            return new CheckResult(false, "插入余量不足，不为身位卡GCD");
        return new CheckResult(true, $"下一刀{SamuraiDebugLog.ActionName(action)}需要{(need == Positional.Rear ? "背" : "侧")}身位");
    }

    public PAction GetAction()
    {
        return new PAction(UniversalData.MeleeUniversalSkill.真北, ActionType.OffGcd, ActionTargetType.Self);
    }
}
