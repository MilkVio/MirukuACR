using PromeRotation.Data;
using PromeRotation.Rotation;
using System.Collections.Generic;
using PromeRotation.Helpers;

using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Opener
{
    public class PLD_90_TOP_ST : IOpener
    {
        public string OpenerName => "骑士绝欧ST起手";

        // 起手
        public List<PAction> InCombatSequence => new()
        {
            new PAction(PLDSkill.先锋剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            new PAction(PLDSkill.暴乱剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            new PAction(PLDSkill.王权剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
        };
        
        // 倒计时 起手
        public void InitializeCountdown(CountDownHandler countdownHandler)
        {
            var 圣灵 = new PAction(PLDSkill.圣灵, ActionType.Gcd, ActionTargetType.Target);
            var 调停 = new PAction(PLDSkill.调停, ActionType.OffGcd, ActionTargetType.Target);
            countdownHandler.AddAction(5000, () => PaladinHelper.AutoIron(false));
            countdownHandler.AddAction(2000, 圣灵);
            countdownHandler.AddAction(500, 调停);
        }
    }
}
