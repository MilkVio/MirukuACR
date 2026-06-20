using PromeRotation.Data;
using PromeRotation.Rotation;
using System.Collections.Generic;
using MilkVio.Tank.GNB.GNBData;


namespace MilkVio.GNB.Opener
{
    /// <summary>
    /// 一个绝枪战士的简单起手示例。
    /// </summary>
    public class Opener_GNB_Simple : IOpener
    {
        public string OpenerName => "绝枪战士测试起手";

        // 起手
        public List<PAction> InCombatSequence => new()
        {
            //new PAction(GNBSkill.弹道, ActionType.OffGcd, Core.Target),
            new PAction(GNBSkill.利刃斩, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            new PAction(GNBSkill.刚玉之心, ActionType.OffGcd, ActionTargetType.Self),
            new PAction(GNBSkill.残暴弹, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            new PAction(GNBSkill.血壤, ActionType.OffGcd, ActionTargetType.Target),
            new PAction(GNBSkill.无情, ActionType.OffGcd, ActionTargetType.Self)
        };
        
        // 倒计时 起手
        public void InitializeCountdown(CountDownHandler countdownHandler)
        {
            var 铁壁 = new PAction(GNBSkill.铁壁, ActionType.OffGcd, ActionTargetType.Self);
            countdownHandler.AddAction(5000, 铁壁);
        }
    }
}
