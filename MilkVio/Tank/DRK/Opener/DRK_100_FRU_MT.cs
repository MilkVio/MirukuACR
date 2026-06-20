using System.Collections.Generic;
using PromeRotation.Data;

namespace MilkVio.Tank.DRK.Opener;

public class DRK_100_FRU_MT : IOpener
{
    public string OpenerName => "暗黑骑士 绝伊甸MT专用起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        new PAction(DRKSkill.暗影步, ActionType.OffGcd, ActionTargetType.Target),
        
        // 1G
        new PAction(DRKSkill.重斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.暗影锋, ActionType.OffGcd, ActionTargetType.Target),
        
        // 2G
        new PAction(DRKSkill.吸收斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.腐秽大地, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(DRKSkill.掠影示现, ActionType.OffGcd, ActionTargetType.Self),
        
        // 3G
        new PAction(DRKSkill.噬魂斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.血乱, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(DRKSkill.暗影锋, ActionType.OffGcd, ActionTargetType.Target),
        
        // 4G
        new PAction(DRKSkill.血红乱, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.精雕怒斩, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(DRKSkill.暗影使者, ActionType.OffGcd, ActionTargetType.Target),
        
        // 5G
        new PAction(DRKSkill.报应, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.腐秽黑暗, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(DRKSkill.暗影锋, ActionType.OffGcd, ActionTargetType.Target),
        
        // 6G
        new PAction(DRKSkill.戮山, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.暗影锋, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(DRKSkill.暗影使者, ActionType.OffGcd, ActionTargetType.Target),
        
        // 7G
        new PAction(DRKSkill.血溅, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.暗影墙, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(DRKSkill.献奉, ActionType.OffGcd, ActionTargetType.Self),
        
        // 8G
        new PAction(DRKSkill.掠影的蔑视, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.弃明投暗, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(DRKSkill.至黑之夜, ActionType.OffGcd, ActionTargetType.Self),
        
        // 9G
        new PAction(DRKSkill.重斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.铁壁, ActionType.OffGcd, ActionTargetType.Self),
        
        // 10G
        new PAction(DRKSkill.吸收斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.暗影锋, ActionType.OffGcd, ActionTargetType.Self),
        
        // 10G
        new PAction(DRKSkill.噬魂斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        //new PAction(DRKSkill.退避, ActionType.OffGcd, ActionTargetType.PartyMember2),
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 黑盾 = new PAction(DRKSkill.至黑之夜, ActionType.OffGcd, ActionTargetType.Self);
        var 挑衅 = new PAction(DRKSkill.挑衅, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(3000, 黑盾);
        countdownHandler.AddAction(500, 挑衅);
    }
}
