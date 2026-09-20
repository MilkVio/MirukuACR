using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using MilkVio.DPS.Dancer.DNCData;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Opener;

public class MCH_100_DMU : IOpener
{
    public string OpenerName => "机工妖星起手";

    public List<PAction> InCombatSequence => new()
    {
        // 0G
        new PAction(MCHSkill.双将, ActionType.OffGcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(MCHSkill.枪管加热, ActionType.OffGcd, ActionTargetType.Self)
        {
            RequiresVerification = true
        },
        
        // 1G
        new PAction(MCHSkill.空气锚, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(MCHSkill.将死, ActionType.OffGcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(MCHSkill.野火, ActionType.OffGcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 2G
        new PAction(MCHSkill.钻头, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.双将, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(MCHSkill.超荷, ActionType.OffGcd, ActionTargetType.Self),
        
        // 3G
        new PAction(MCHSkill.烈焰弹, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.将死, ActionType.OffGcd, ActionTargetType.Target),
        
        // 4G
        new PAction(MCHSkill.烈焰弹, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.双将, ActionType.OffGcd, ActionTargetType.Target),
        
        // 5G
        new PAction(MCHSkill.烈焰弹, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.将死, ActionType.OffGcd, ActionTargetType.Target),
        
        // 6G
        new PAction(MCHSkill.烈焰弹, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.双将, ActionType.OffGcd, ActionTargetType.Target),
        
        // 7G
        new PAction(MCHSkill.烈焰弹, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.整备, ActionType.OffGcd, ActionTargetType.Self),
        
        // 8G
        new PAction(MCHSkill.掘地飞轮, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.后式自走人偶, ActionType.OffGcd, ActionTargetType.Self),
        
    };
    
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 整备 = new PAction(MCHSkill.整备, ActionType.OffGcd, ActionTargetType.Self);
        var 回转飞锯 = new PAction(MCHSkill.回转飞锯, ActionType.Gcd, ActionTargetType.Target);
        
        countdownHandler.AddAction(4500, 整备);
        countdownHandler.AddAction(2000, () => new PAction(GameData.GetBestPotionId(), ActionType.Item, ActionTargetType.Self));
        countdownHandler.AddAction(400, 回转飞锯);
    }
}
