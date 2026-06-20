using System.Collections.Generic;
using PromeRotation.Data;

namespace MilkVio.Tank.DRK.Opener;

public class DRK_90_TOP_MT : IOpener
{
    public string OpenerName => "暗黑骑士 绝欧米茄MT起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(DRKSkill.重斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.暗影锋, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(DRKSkill.掠影示现, ActionType.OffGcd, ActionTargetType.Self),
        
        // 2G
        new PAction(DRKSkill.吸收斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.腐秽大地, ActionType.OffGcd, ActionTargetType.Self),
        
        // 3G
        new PAction(DRKSkill.噬魂斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.血乱, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(DRKSkill.暗影锋, ActionType.OffGcd, ActionTargetType.Target),
        
        // 4G
        new PAction(DRKSkill.血溅, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.暗影使者, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(DRKSkill.精雕怒斩, ActionType.OffGcd, ActionTargetType.Target),
        
        // 5G
        new PAction(DRKSkill.血溅, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.暗影锋, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(DRKSkill.腐秽黑暗, ActionType.OffGcd, ActionTargetType.Target),
        
        // 6G
        new PAction(DRKSkill.血溅, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.至黑之夜, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(DRKSkill.暗影使者, ActionType.OffGcd, ActionTargetType.Target),
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 黑盾 = new PAction(DRKSkill.至黑之夜, ActionType.OffGcd, ActionTargetType.Self);
        var 献奉 = new PAction(DRKSkill.献奉, ActionType.OffGcd, ActionTargetType.Self);
        var 突进 = new PAction(DRKSkill.暗影步, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(3000, 黑盾);
        countdownHandler.AddAction(2000, 献奉);
        countdownHandler.AddAction(800, 突进);
    }
}
