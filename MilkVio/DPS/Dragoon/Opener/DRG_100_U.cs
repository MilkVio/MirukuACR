using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using MilkVio.DPS.Dragoon.DRGData;

namespace MilkVio.DPS.Dragoon.Opener;

public class DRG_100_U : IOpener
{
    public string OpenerName => "龙骑士100通用起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(DRGSkill.精准刺,ActionType.Gcd,ActionTargetType.Target){
            RequiresVerification = true
        },
        new PAction(UniversalData.MeleeUniversalSkill.真北,ActionType.OffGcd,ActionTargetType.Self),
        
        // 2G
        new PAction(DRGSkill.螺旋击,ActionType.Gcd,ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(DRGSkill.猛枪,ActionType.OffGcd,ActionTargetType.Self),
        new PAction(GameData.GetBestPotionId(),ActionType.Item,ActionTargetType.Self),
        
        // 3G
        new PAction(DRGSkill.樱花缭乱,ActionType.Gcd,ActionTargetType.Target){
            RequiresVerification = true
        },
        new PAction(DRGSkill.战斗连祷,ActionType.OffGcd,ActionTargetType.Self),
        new PAction(DRGSkill.武神枪,ActionType.OffGcd,ActionTargetType.Target),
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        countdownHandler.AddAction(500, new PAction(DRGSkill.龙翼滑翔, ActionType.OffGcd, ActionTargetType.Target));
    }
}
