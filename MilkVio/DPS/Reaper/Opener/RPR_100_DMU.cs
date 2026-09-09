using System.Collections.Generic;
using MilkVio.DPS.Reaper.ReaperData;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.DPS.Reaper.Opener;

public class RPR_100_DMU : IOpener
{
    public string OpenerName => "镰刀100级妖星起手";

    public void InitializeCountdown(CountDownHandler countdownHandler)
        => ReaperBattleData.Instance.DmuOpener.BeginCountdown();

    // 开战时宿主会重新创建IOpener，进度保存在BattleData中，不能从这里重新预读。
    public List<PAction> InCombatSequence
    {
        get
        {
            ReaperBattleData.Instance.DmuOpener.BeginCombat();
            return new();
        }
    }
}
