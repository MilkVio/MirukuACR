using System.Collections.Generic;
using PromeRotation.Data;
using MilkVio.Tank.WAR.WARData;

namespace MilkVio.Tank.WAR.Opener;

public class WAR_100_DMU_MT : IOpener
{
    public string OpenerName => "战士绝妖星MT起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(WARSkill.重劈, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.铁壁, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(WARSkill.战嚎, ActionType.OffGcd, ActionTargetType.Self),
        
        // 2G
        new PAction(WARSkill.凶残裂, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.挑衅, ActionType.OffGcd, ActionTargetType.Target),
        
        // 3G
        new PAction(WARSkill.暴风碎, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.原初的解放, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(WARSkill.动乱, ActionType.OffGcd, ActionTargetType.Target),
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 猛攻 = new PAction(WARSkill.猛攻, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(5000, () => WarriorHelper.AutoGuard(true));
        countdownHandler.AddAction(500, 猛攻);
    }
}
