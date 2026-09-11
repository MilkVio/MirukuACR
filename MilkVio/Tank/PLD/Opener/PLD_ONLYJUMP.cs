using PromeRotation.Data;
using PromeRotation.Rotation;
using System.Collections.Generic;
using PromeRotation.Helpers;

using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Opener
{
    public class PLD_ONLYJUMP : IOpener
    {
        public string OpenerName => "骑士ST突进起手";

        // 起手
        public List<PAction> InCombatSequence => new()
        {
            
        };
        
        // 倒计时 起手
        public void InitializeCountdown(CountDownHandler countdownHandler)
        {
            var 圣灵 = new PAction(PLDSkill.圣灵, ActionType.Gcd, ActionTargetType.Target);
            var 调停 = new PAction(PLDSkill.调停, ActionType.OffGcd, ActionTargetType.Target);
            countdownHandler.AddAction(5000, () => PaladinHelper.AutoIron(false));
            countdownHandler.AddAction(1800, 圣灵);
            countdownHandler.AddAction(300, 调停);
        }
    }
}
