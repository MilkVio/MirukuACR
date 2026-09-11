using System.Collections.Generic;
using PromeRotation.Data;

namespace MilkVio.Tank.DRK.Opener;

public class DRK_ONLY_JUMP : IOpener
{
    public string OpenerName => "暗黑骑士MT单突进起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 突进 = new PAction(DRKSkill.暗影步, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(5000, () => DarkKnightHelper.AutoMalice(true));
        countdownHandler.AddAction(500, 突进);
    }
}
