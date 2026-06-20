using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using MilkVio.DPS.Dancer.DNCData;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Opener;

public class MCH_80_TEADS : IOpener
{
    public string OpenerName => "机工80绝亚DollSkip起手";

    public List<PAction> InCombatSequence => new()
    {
        
    };
    
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 整备 = new PAction(MCHSkill.整备, ActionType.OffGcd, ActionTargetType.Self);
        var 空气锚 = new PAction(MCHSkill.空气锚, ActionType.Gcd, ActionTargetType.Target);
        countdownHandler.AddAction(4500, 整备);
        countdownHandler.AddAction(400, 空气锚);
    }
}
