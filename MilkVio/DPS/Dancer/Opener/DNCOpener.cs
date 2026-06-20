using System.Collections.Generic; 
using PromeRotation.Data; 
using PromeRotation.Helpers;
using PromeRotation.Rotation;
using MilkVio.DPS.Dancer.DNCData; 

public class DNCopener : IOpener
{                                           
    // 这里要写成Opener↑
    public string OpenerName => "通用起手"; // 倒计时 起手 这里要写对应的副本名称 是单独对副本的特化还是通用

    public List<PAction> InCombatSequence => new()
    {
      
    };
    
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 小舞 = new PAction(DNCSkill.标准舞步, ActionType.Gcd, ActionTargetType.Self);
        var 小舞结束 = new PAction(DNCSkill.标准舞步结束, ActionType.Gcd, ActionTargetType.Self);
        countdownHandler.AddAction(15000, 小舞);
        countdownHandler.AddAction(13500, () => new PAction(JobGaugeHelper.DNC.NextStep, ActionType.Gcd, ActionTargetType.Self));
        countdownHandler.AddAction(12500, () => new PAction(JobGaugeHelper.DNC.NextStep, ActionType.Gcd, ActionTargetType.Self));
        countdownHandler.AddAction(25,小舞结束);
    }
}
