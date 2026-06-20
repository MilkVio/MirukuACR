using System.Collections.Generic;
using PromeRotation.Data;
using MilkVio.DPS.Monk.MNKData;

namespace MilkVio.DPS.Monk.Opener;

public class MNK_UWU_U : IOpener
{
    public string OpenerName => "神兵起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(MNKSkill.双龙脚, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        // new PAction(MNKSkill.震脚, ActionType.OffGcd, ActionTargetType.Self),
        
        // 2G
        new PAction(MNKSkill.双掌打, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(MNKSkill.阴阳斗气斩, ActionType.OffGcd, ActionTargetType.Target),
        
        // 3G
        new PAction(MNKSkill.破碎拳, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 4G
        new PAction(MNKSkill.连击, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(MNKSkill.震脚, ActionType.OffGcd, ActionTargetType.Self),
        // 5G
        new PAction(MNKSkill.双龙脚, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(MNKSkill.义结金兰, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(MNKSkill.红莲极意, ActionType.OffGcd, ActionTargetType.Self),
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 演武 = new PAction(MNKSkill.演武, ActionType.Gcd, ActionTargetType.Self);
        var 搓豆子 = new PAction(MNKSkill.搓豆子, ActionType.Gcd, ActionTargetType.Self);
        var 轻身 = new PAction(MNKSkill.轻身步法, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(5000, 演武);
        countdownHandler.AddAction(2000, 搓豆子);
        countdownHandler.AddAction(500, 轻身);
    }
}
