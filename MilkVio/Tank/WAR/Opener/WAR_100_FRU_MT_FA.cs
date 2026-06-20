using System.Collections.Generic;
using PromeRotation.Data;
using MilkVio.Tank.WAR.WARData;

namespace MilkVio.Tank.WAR.Opener;

public class WAR_100_FRU_MT_FA : IOpener
{
    public string OpenerName => "战士绝伊甸MT专用起手2";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        new PAction(WARSkill.猛攻, ActionType.OffGcd, ActionTargetType.Target),
        
        // 1G
        new PAction(WARSkill.重劈, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.原初的解放, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(WARSkill.动乱, ActionType.OffGcd, ActionTargetType.Target),
        
        // 2G
        new PAction(WARSkill.凶残裂, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.战嚎, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(WARSkill.猛攻, ActionType.OffGcd, ActionTargetType.Target),
        
        // 3G
        new PAction(WARSkill.暴风碎, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.猛攻, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(WARSkill.血仇, ActionType.OffGcd, ActionTargetType.Self),
        
        // 4G
        new PAction(WARSkill.狂魂, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 5G 裂石飞环1
        new PAction(WARSkill.裂石飞环, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 6G 裂石飞环2
        new PAction(WARSkill.裂石飞环, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 7G 裂石飞环3
        new PAction(WARSkill.裂石飞环, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.原初的怒震, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(WARSkill.戮罪, ActionType.OffGcd, ActionTargetType.Self),
        
        // 8G 
        new PAction(WARSkill.蛮荒崩裂, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.战栗, ActionType.OffGcd, ActionTargetType.Self),
        
        // 9G
        new PAction(WARSkill.尽毁, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.铁壁, ActionType.OffGcd, ActionTargetType.Self),
        
        // 10G
        new PAction(WARSkill.重劈, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WARSkill.战嚎, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(WARSkill.退避, ActionType.OffGcd, ActionTargetType.PartyMember2),
        
        new PAction(WARSkill.狂魂, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        new PAction(WARSkill.凶残裂, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        new PAction(WARSkill.暴风斩, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        new PAction(WARSkill.裂石飞环, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 挑衅 = new PAction(WARSkill.挑衅, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(5000, () => WarriorHelper.AutoGuard(true));
        countdownHandler.AddAction(300, 挑衅);
    }
}
