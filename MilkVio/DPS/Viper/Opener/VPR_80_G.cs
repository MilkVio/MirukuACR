using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using MilkVio.DPS.UniversalData;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Opener;

public class VPR_80_G : IOpener
{
    public string OpenerName => "蝰蛇80级通用起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(ViperSkill.穿裂尖齿右1, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
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
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 蛇行 = new PAction(ViperSkill.蛇行, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(300, 蛇行);
    }
}
