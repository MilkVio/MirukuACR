using PromeRotation.Data;
using PromeRotation.Rotation;
using System.Collections.Generic;
using PromeRotation.Helpers;

using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Opener
{
    public class PLD_100_FRU_ST_FA : IOpener
    {
        public string OpenerName => "骑士绝伊甸ST起手2";

        // 起手
        public List<PAction> InCombatSequence => new()
        {
            new PAction(PLDSkill.先锋剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.厄运流转, ActionType.OffGcd, ActionTargetType.Self),
            new PAction(PLDSkill.偿赎剑, ActionType.OffGcd, ActionTargetType.Target),
            
            new PAction(PLDSkill.暴乱剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.钢铁信念, ActionType.OffGcd, ActionTargetType.Self),
            
            new PAction(PLDSkill.圣灵, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            new PAction(PLDSkill.圣灵, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.王权剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.战逃反应, ActionType.OffGcd, ActionTargetType.Self),
            new PAction(PaladinHelper.GetAdjustedAnhunId(), ActionType.OffGcd, ActionTargetType.Target),
            
            new PAction(PLDSkill.悔罪, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.调停, ActionType.OffGcd, ActionTargetType.Target),
            
            new PAction(PLDSkill.信念之剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.真理之剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.干预, ActionType.OffGcd, ActionTargetType.PartyMember2),
            new PAction(7533, ActionType.OffGcd, ActionTargetType.Target),
            
            new PAction(PLDSkill.英勇之剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.荣耀之剑, ActionType.OffGcd, ActionTargetType.Target),
            
            new PAction(PLDSkill.沥血剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.赎罪剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.祈告剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.调停, ActionType.OffGcd, ActionTargetType.Target),
            
            new PAction(PLDSkill.葬送剑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            new PAction(PLDSkill.厄运流转, ActionType.OffGcd, ActionTargetType.Self),
            new PAction(PLDSkill.偿赎剑, ActionType.OffGcd, ActionTargetType.Target),
            
            new PAction(PLDSkill.圣灵, ActionType.Gcd, ActionTargetType.Target)
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
            countdownHandler.AddAction(1800, 圣灵);
            countdownHandler.AddAction(300, 调停);
        }
    }
}
