using System.Collections.Generic;
using PromeRotation.Data;

namespace MilkVio.Tank.DRK.Opener;

public class DRK_80_TEADS_ST : IOpener
{
    public string OpenerName => "暗黑骑士绝亚DollSkip起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(DRKSkill.重斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.暗影锋, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(DRKSkill.吸收斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRKSkill.噬魂斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 突进 = new PAction(DRKSkill.暗影步, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(5000, () => DarkKnightHelper.AutoMalice(false));
        countdownHandler.AddAction(500, 突进);
    }
}
