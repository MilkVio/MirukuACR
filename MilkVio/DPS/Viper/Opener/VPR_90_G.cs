using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using MilkVio.DPS.UniversalData;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Opener;

public class VPR_90_G : IOpener
{
    public string OpenerName => "蝰蛇90级通用起手";

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
        new PAction(7863, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(GameData.GetBestPotionId(),ActionType.Item,ActionTargetType.Self),
        
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
        new PAction(ViperSkill.侧裂獠齿右3红, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(ViperSkill.蛇尾击, ActionType.OffGcd, ActionTargetType.Target),
        
        // 7G
        new PAction(ViperSkill.祖灵降临, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 蛇行 = new PAction(ViperSkill.蛇行, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(300, 蛇行);
    }
}
