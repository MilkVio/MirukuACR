using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using MilkVio.DPS.UniversalData;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Opener;

public class VPR_100_DMU : IOpener
{
    public string OpenerName => "蝰蛇100级妖星起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(ViperSkill.穿裂尖齿右1, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(ViperSkill.蛇灵气, ActionType.OffGcd, ActionTargetType.Self),
        
        // 2G
        new PAction(ViperSkill.急速利齿右2, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 3G
        new PAction(ViperSkill.强碎灵蛇, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(MeleeUniversalSkill.真北, ActionType.OffGcd, ActionTargetType.Target),
        
        // 4G
        new PAction(ViperSkill.猛袭盘蛇, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(ViperSkill.双牙连击, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(ViperSkill.双牙乱击, ActionType.OffGcd, ActionTargetType.Target),
        
        // 5G
        new PAction(ViperSkill.急速盘蛇, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(ViperSkill.双牙乱击, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(ViperSkill.双牙连击, ActionType.OffGcd, ActionTargetType.Target),
        
        // 6G
        new PAction(ViperSkill.祖灵降临, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 7G
        new PAction(ViperSkill.祖灵之牙一式, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(34640, ActionType.OffGcd, ActionTargetType.Target),
        
        // 8G
        new PAction(ViperSkill.祖灵之牙二式, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(34641, ActionType.OffGcd, ActionTargetType.Target),
        
        // 9G
        new PAction(ViperSkill.祖灵之牙三式, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(34642, ActionType.OffGcd, ActionTargetType.Target),
        
        // 10G
        new PAction(ViperSkill.祖灵之牙四式, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(34643, ActionType.OffGcd, ActionTargetType.Target),
        
        // 11G
        new PAction(34631, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 12G
        new PAction(ViperSkill.强碎灵蛇, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 蛇行 = new PAction(ViperSkill.蛇行, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(500, 蛇行);
    }
}
