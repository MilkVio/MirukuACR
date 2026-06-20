using System.Collections.Generic;
using PromeRotation.Data;
using MilkVio.Tank.WAR.WARData;

namespace MilkVio.Tank.WAR.Opener;

public class WAR_90_TOP_MT : IOpener
{
    public string OpenerName => "战士绝欧MT起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(WARSkill.重劈, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.战嚎, ActionType.OffGcd, ActionTargetType.Self),
        
        // 2G
        new PAction(WARSkill.凶残裂, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.原初的血气, ActionType.OffGcd, ActionTargetType.Self),
        
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
        var 铁壁 = new PAction(WARSkill.铁壁, ActionType.OffGcd, ActionTargetType.Self);
        var 复仇 = new PAction(WARSkill.复仇, ActionType.OffGcd, ActionTargetType.Self);
        var 猛攻 = new PAction(WARSkill.猛攻, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(7000, () => WarriorHelper.AutoGuard(true));
        countdownHandler.AddAction(5000, 铁壁);
        countdownHandler.AddAction(3000, 复仇);
        countdownHandler.AddAction(500, 猛攻);
    }
}
