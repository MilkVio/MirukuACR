using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Opener;

public class SMN_100_FRU : IOpener
{
    public string OpenerName => "召唤绝伊甸起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(SMNSkill.烈日龙神召唤, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(GameData.GetBestPotionId(), ActionType.Item, ActionTargetType.Self),
        
        // 2G
        new PAction(SMNSkill.灵极脉冲, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        new PAction(SMNSkill.灼热之光, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(SMNSkill.能量吸收, ActionType.OffGcd, ActionTargetType.Target),
        
        // 3G
        new PAction(SMNSkill.灵极脉冲, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 4G
        new PAction(SMNSkill.灵极脉冲, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(SMNSkill.烈日核爆, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(SMNSkill.烈日龙神迸发, ActionType.OffGcd, ActionTargetType.Target),
        
        // 5G
        new PAction(SMNSkill.灵极脉冲, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(SMNSkill.坏死爆发, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(SMNSkill.灼热之闪, ActionType.OffGcd, ActionTargetType.Target),
        
        // 6G
        new PAction(SMNSkill.灵极脉冲, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(SMNSkill.坏死爆发, ActionType.OffGcd, ActionTargetType.Target),
        
        // 7G
        new PAction(SMNSkill.灵极脉冲, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 8G
        new PAction(SMNSkill.火神召唤, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(SMNSkill.即刻咏唱, ActionType.OffGcd, ActionTargetType.Self),
        
        // 9G
        new PAction(SMNSkill.深红旋风, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 10G
        new PAction(25823, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 11G
        new PAction(SMNSkill.土神召唤, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 12G
        new PAction(25824, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(25836, ActionType.OffGcd, ActionTargetType.Target),
        
        // 13G
        new PAction(25824, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(25836, ActionType.OffGcd, ActionTargetType.Target),
        
        // 14G
        new PAction(SMNSkill.毁绝, ActionType.Gcd, ActionTargetType.Target),
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 毁荡 = new PAction(SMNSkill.毁荡, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(5000, () => SummonerHelper.AutoPetSummon());
        countdownHandler.AddAction(1500, 毁荡);
    }
}
