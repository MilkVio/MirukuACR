using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using MilkVio.DPS.Ninja.NinjaData;
using MilkVio.DPS.Samurai.SAMData;
using MilkVio.DPS.UniversalData;

namespace MilkVio.DPS.Samurai.Opener;

public class SAM_80_TEADS : IOpener
{
    public string OpenerName => "武士绝亚DollSkip起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(SAMSkill.月光, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 2G
        new PAction(SAMSkill.彼岸花, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 3G
        new PAction(SAMSkill.花车, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 4G
        new PAction(SAMSkill.月光, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 明镜 = new PAction(SAMSkill.明镜止水, ActionType.OffGcd, ActionTargetType.Self);
        var 真北 = new PAction(MeleeUniversalSkill.真北, ActionType.OffGcd, ActionTargetType.Self);
        countdownHandler.AddAction(5000, 明镜);
        countdownHandler.AddAction(2000, 真北);
    }
}
