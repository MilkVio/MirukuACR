using System.Collections.Generic;
using PromeRotation.Data;
using MilkVio.DPS.Monk.MNKData;

namespace MilkVio.DPS.Monk.Opener;

public class MNK_80_TEADS : IOpener
{
    public string OpenerName => "武僧绝亚DollSkip起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(MNKSkill.双龙脚, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        }
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
