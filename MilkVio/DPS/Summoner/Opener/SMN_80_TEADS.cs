using System.Collections.Generic;
using PromeRotation.Core;
using PromeRotation.Data;
using MilkVio.DPS.Ninja.NinjaData;
using MilkVio.DPS.Samurai.SAMData;
using MilkVio.DPS.Summoner.SMNData;
using MilkVio.DPS.UniversalData;

namespace MilkVio.DPS.Summoner.Opener;

public class SMN_80_TEADS : IOpener
{
    public string OpenerName => "召唤绝亚DollSkip起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 宝石兽召唤 = new PAction(SMNSkill.宝石兽召唤, ActionType.OffGcd, ActionTargetType.Self);
        var 毁荡 = new PAction(SMNSkill.毁荡, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(5000, 宝石兽召唤);
        countdownHandler.AddAction(2000, 毁荡);
    }
}
