using System.Collections.Generic; 
using PromeRotation.Data; 
using PromeRotation.Helpers;
using PromeRotation.Rotation;
using MilkVio.Healer.WHM.WHMData;

public class WHM_90_TOP : IOpener
{                                           
    // 这里要写成Opener↑
    public string OpenerName => "白魔90级绝欧起手";

    public List<PAction> InCombatSequence => new()
    {
        new PAction(WHMSkill.天辉, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(WHMSkill.法令, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(WHMSkill.神速咏唱, ActionType.OffGcd, ActionTargetType.Self),
    };
    
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 闪灼 = new PAction(WHMSkill.闪灼, ActionType.Gcd, ActionTargetType.Target);
        countdownHandler.AddAction(1300, 闪灼);
    }
}
