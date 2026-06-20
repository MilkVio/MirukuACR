using System.Collections.Generic;
using PromeRotation.Data;
using MilkVio.Tank.WAR.WARData;

namespace MilkVio.Tank.WAR.Opener;

public class WAR_80_TEADS_MT : IOpener
{
    public string OpenerName => "战士绝亚DollSkip_MT起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(WARSkill.重劈, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        }
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 猛攻 = new PAction(WARSkill.猛攻, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(7000, () => WarriorHelper.AutoGuard(true));
        countdownHandler.AddAction(500, 猛攻);
    }
}
