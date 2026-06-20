using System.Collections.Generic;
using PromeRotation.Data;
using MilkVio.DPS.Ninja.NinjaData;
using MilkVio.DPS.UniversalData;

namespace MilkVio.DPS.Ninja.Opener;

public class NIN_90_U : IOpener
{
    public string OpenerName => "忍者90通用2G爆发起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(NinjaSkill.双刃旋, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        // 这里可以吃爆发药
        new PAction(NinjaSkill.生杀, ActionType.OffGcd, ActionTargetType.Self),
        
        // 2G
        new PAction(NinjaSkill.绝风, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(NinjaSkill.介毒之术, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(NinjaSkill.分身之术, ActionType.OffGcd, ActionTargetType.Self),
        
        // 3G
        new PAction(NinjaSkill.残影镰鼬, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(MeleeUniversalSkill.真北, ActionType.OffGcd, ActionTargetType.Self),
        
        // 4G
        new PAction(NinjaSkill.强甲破点突, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(7863, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(NinjaSkill.攻其不备, ActionType.OffGcd, ActionTargetType.Target),
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 天 = new PAction(NinjaSkill.天之印, ActionType.Gcd, ActionTargetType.Self);
        var 地 = new PAction(NinjaSkill.地之印派生, ActionType.Gcd, ActionTargetType.Self);
        var 人 = new PAction(NinjaSkill.人之印派生, ActionType.Gcd, ActionTargetType.Self);
        var 忍术 =  new PAction(NinjaSkill.忍术, ActionType.Gcd, ActionTargetType.Target);
        countdownHandler.AddAction(5000, 天);
        countdownHandler.AddAction(4500, 地);
        countdownHandler.AddAction(4000, 人);
        countdownHandler.AddAction(500, 忍术);
    }
}
