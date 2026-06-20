using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Helpers;
using MilkVio.DPS.Dancer.DNCData;
using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Opener;

public class MCH_90_OMG : IOpener
{
    public string OpenerName => "机工90绝欧起手";

    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(MCHSkill.钻头, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.整备, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(MCHSkill.枪管加热, ActionType.OffGcd, ActionTargetType.Self),
        
        new PAction(MCHSkill.回转飞锯, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.虹吸弹, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(MCHSkill.弹射, ActionType.OffGcd, ActionTargetType.Target),
        
        new PAction(MCHSkill.热分裂弹1, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.虹吸弹, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(MCHSkill.弹射, ActionType.OffGcd, ActionTargetType.Target),
        
        new PAction(MCHSkill.热独头弹2, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.虹吸弹, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(MCHSkill.野火, ActionType.OffGcd, ActionTargetType.Target),
        
        new PAction(MCHSkill.热狙击弹3, ActionType.Gcd, ActionTargetType.Target),
        new PAction(MCHSkill.超荷, ActionType.OffGcd, ActionTargetType.Self),
        new PAction(MachinistHelper.GetCurrentRobotActionId(), ActionType.OffGcd, ActionTargetType.Self),
    };
    
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 整备 = new PAction(MCHSkill.整备, ActionType.OffGcd, ActionTargetType.Self);
        var 空气锚 = new PAction(MCHSkill.空气锚, ActionType.Gcd, ActionTargetType.Target);
        countdownHandler.AddAction(4500, 整备);
        countdownHandler.AddAction(2000, () => new PAction(GameData.GetBestPotionId(), ActionType.Item, ActionTargetType.Self));
        countdownHandler.AddAction(400, 空气锚);
    }
}
