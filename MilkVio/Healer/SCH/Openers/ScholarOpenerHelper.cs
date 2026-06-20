using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Rotation;
using PromeGameData = PromeRotation.Core.GameData;

namespace MilkVio.Healer.SCH.Openers;

internal static class ScholarOpenerHelper
{
    public static List<PAction> Ultimate()
    {
        var actions = new List<PAction>();

        if (PromeSettings.Instance.GetQt(HealerQt.DOT))
            AddGcd(actions, SchAction.毒菌);

        AddResourceOpener(actions);
        AddGcd(actions, SchAction.毁灭);

        if (MioAcrSettings.Instance.SchSwiftOpenerIndex == 0)
        {
            AddOffGcd(actions, RoleAction.即刻咏唱, ActionTargetType.Self);
            AddGcd(actions, SchAction.毁灭);
            AddQtOffGcd(actions, HealerQt.连环计, SchAction.连环计, ActionTargetType.Target);
        }
        else
        {
            AddGcd(actions, SchAction.毁灭);
            AddQtOffGcd(actions, HealerQt.连环计, SchAction.连环计, ActionTargetType.Target);
            AddGcd(actions, SchAction.毁灭);
        }

        return actions;
    }

    public static void InitializeCountdown(CountDownHandler countdownHandler)
    {
        AddSummonEos(countdownHandler);
        AddShieldOpener(countdownHandler);
        AddPotion(countdownHandler);
        countdownHandler.AddAction(2500, new PAction(SchAction.毁灭, ActionType.Gcd, ActionTargetType.Target));
    }

    private static void AddSummonEos(CountDownHandler countdownHandler)
    {
        if (!PromeSettings.Instance.GetQt(HealerQt.朝日召唤)) return;
        if (HealerUtils.SchHasPet() != false) return;

        countdownHandler.AddAction(18000, new PAction(SchAction.朝日召唤, ActionType.Gcd, ActionTargetType.Self));
    }

    private static void AddShieldOpener(CountDownHandler countdownHandler)
    {
        switch (Math.Clamp(MioAcrSettings.Instance.SchShieldOpenerIndex, 0, 2))
        {
            case 0:
                countdownHandler.AddAction(15000, new PAction(SchAction.异想的幻光, ActionType.OffGcd, ActionTargetType.Self));
                countdownHandler.AddAction(10000, new PAction(SchAction.秘策, ActionType.OffGcd, ActionTargetType.Self));
                countdownHandler.AddAction(9000, new PAction(SchAction.生命回生法, ActionType.OffGcd, ActionTargetType.Self));
                countdownHandler.AddAction(8000, new PAction(SchAction.鼓舞激励之策, ActionType.Gcd, ActionTargetType.Self));
                countdownHandler.AddAction(5000, new PAction(SchAction.展开战术, ActionType.OffGcd, ActionTargetType.Self));
                break;
            case 2:
                countdownHandler.AddAction(10000, new PAction(SchAction.意气轩昂之策, ActionType.Gcd, ActionTargetType.Self));
                break;
        }
    }

    private static void AddPotion(CountDownHandler countdownHandler)
    {
        if (!PromeSettings.Instance.GetQt(HealerQt.爆发药)) return;

        var potionId = PromeGameData.GetBestPotionId();
        if (potionId != 0)
            countdownHandler.AddAction(3000, new PAction(potionId, ActionType.Item, ActionTargetType.Self));
    }

    private static void AddResourceOpener(List<PAction> actions)
    {
        var actionId = Math.Clamp(MioAcrSettings.Instance.SchResourceOpenerIndex, 0, 1) == 0
            ? SchAction.转化
            : SchAction.以太超流;
        var qt = actionId == SchAction.转化 ? HealerQt.转化 : HealerQt.以太超流;

        AddQtOffGcd(actions, qt, actionId, ActionTargetType.Self);
    }

    private static void AddGcd(List<PAction> actions, uint actionId)
        => actions.Add(new PAction(actionId, ActionType.Gcd, ActionTargetType.Target));

    private static void AddOffGcd(List<PAction> actions, uint actionId, ActionTargetType target)
        => actions.Add(new PAction(actionId, ActionType.OffGcd, target));

    private static void AddQtOffGcd(List<PAction> actions, string qt, uint actionId, ActionTargetType target)
    {
        if (PromeSettings.Instance.GetQt(qt))
            AddOffGcd(actions, actionId, target);
    }
}
