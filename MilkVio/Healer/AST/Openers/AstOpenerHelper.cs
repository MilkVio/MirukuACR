using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Rotation;
using PromeGameData = PromeRotation.Core.GameData;

namespace MilkVio.Healer.AST.Openers;

internal static class AstOpenerHelper
{
    public static List<PAction> DotOnly()
    {
        var actions = new List<PAction>();
        AddDot(actions);
        return actions;
    }

    public static List<PAction> Standard()
    {
        var actions = new List<PAction>();

        AddDot(actions);
        AddQtOffGcd(actions, HealerQt.光速, AstAction.光速, ActionTargetType.Self);
        AddGcd(actions, AstAction.凶星);
        AddGcd(actions, AstAction.凶星);
        AddQtOffGcd(actions, HealerQt.占卜, AstAction.占卜, ActionTargetType.Self);
        AddAstCard(actions, AstAction.出卡太阳, "Balance");
        AddGcd(actions, AstAction.凶星);
        AddQtOffGcd(actions, HealerQt.剑卡, AstAction.王冠之领主, ActionTargetType.Self);
        AddQtOffGcd(actions, HealerQt.抽卡, AstAction.灵极抽卡, ActionTargetType.Self);
        AddGcd(actions, AstAction.凶星);
        AddAstCard(actions, AstAction.出卡月亮, "Spear");
        AddQtOffGcd(actions, HealerQt.占卜, AstAction.神谕, ActionTargetType.Target);

        return actions;
    }

    public static List<PAction> EdenTwo()
    {
        var actions = new List<PAction>
        {
            new(AstAction.凶星, ActionType.Gcd, ActionTargetType.Target),
        };

        AddGcd(actions, AstAction.凶星);
        AddQtOffGcd(actions, HealerQt.占卜, AstAction.占卜, ActionTargetType.Self);
        AddDot(actions);
        AddQtOffGcd(actions, HealerQt.光速, AstAction.光速, ActionTargetType.Self);
        AddGcd(actions, AstAction.凶星);
        AddGcd(actions, AstAction.凶星);
        AddQtOffGcd(actions, HealerQt.占卜, AstAction.占卜, ActionTargetType.Self);

        return actions;
    }

    public static void InitializeStandardCountdown(CountDownHandler countdownHandler, bool addPrecast)
    {
        AddEarthlyStar(countdownHandler);

        if (addPrecast)
        {
            AddPotion(countdownHandler);
            countdownHandler.AddAction(2500, new PAction(AstAction.凶星, ActionType.Gcd, ActionTargetType.Target));
        }
    }

    public static void InitializeEdenCountdown(CountDownHandler countdownHandler)
    {
        AddEarthlyStar(countdownHandler);
        countdownHandler.AddAction(8000, new PAction(AstAction.天宫图, ActionType.OffGcd, ActionTargetType.Self));

        if (MioAcrSettings.Instance.AstNeutralSectBeforePull)
            countdownHandler.AddAction(7000, new PAction(AstAction.中间学派, ActionType.OffGcd, ActionTargetType.Self));

        countdownHandler.AddAction(5500, new PAction(AstAction.太阳星座, ActionType.OffGcd, ActionTargetType.Self));
        AddPotion(countdownHandler);
        countdownHandler.AddAction(2500, new PAction(AstAction.凶星, ActionType.Gcd, ActionTargetType.Target));
    }

    private static void AddEarthlyStar(CountDownHandler countdownHandler)
    {
        if (PromeSettings.Instance.GetQt(HealerQt.倒计时地星))
            countdownHandler.AddAction(8000, EarthlyStarAtSelf);
    }

    private static PAction EarthlyStarAtSelf()
    {
        return new PAction(AstAction.地星, ActionType.OffGcd, ActionTargetType.Self)
        {
            IsLocationAction = true,
            Position = HealerUtils.Me?.Position ?? default,
        };
    }

    private static void AddPotion(CountDownHandler countdownHandler)
    {
        if (!PromeSettings.Instance.GetQt(HealerQt.爆发药)) return;

        var potionId = PromeGameData.GetBestPotionId();
        if (potionId != 0)
            countdownHandler.AddAction(3000, new PAction(potionId, ActionType.Item, ActionTargetType.Self));
    }

    private static void AddDot(List<PAction> actions)
    {
        if (PromeSettings.Instance.GetQt(HealerQt.DOT))
            AddGcd(actions, AstAction.焚灼);
    }

    private static void AddGcd(List<PAction> actions, uint actionId)
        => actions.Add(new PAction(actionId, ActionType.Gcd, ActionTargetType.Target));

    private static void AddQtOffGcd(List<PAction> actions, string qt, uint actionId, ActionTargetType target)
    {
        if (PromeSettings.Instance.GetQt(qt))
            actions.Add(new PAction(actionId, ActionType.OffGcd, target));
    }

    private static void AddAstCard(List<PAction> actions, uint actionId, string card)
    {
        if (PromeSettings.Instance.GetQt(HealerQt.发卡))
            actions.Add(new PAction(actionId, ActionType.OffGcd, HealerUtils.BestAstCardTarget(card)));
    }
}
