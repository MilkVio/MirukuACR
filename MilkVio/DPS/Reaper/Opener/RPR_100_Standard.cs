using MilkVio.DPS.Reaper.ReaperData;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Rotation;

namespace MilkVio.DPS.Reaper.Opener;

public class RPR_100_Standard : IOpener
{
    public string OpenerName => "镰刀100级标准起手";

    // 宿主在开战时读取此属性。战斗内由同一个动态规划器逐招执行，不排不可撤销的长队列。
    public List<PAction> InCombatSequence
    {
        get
        {
            ReaperBattleData.Instance.Planner.BeginOpener(ReaperState.Read());
            return new List<PAction>();
        }
    }

    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var harpeAt = (int)MathF.Round(ReaperSettings.Instance.勾刃倒数预读时间 * 1000);
        countdownHandler.AddAction(harpeAt, () =>
        {
            if (!ReaperSettings.Instance.启用起手 || PromeSettings.Instance.EnableAcr is AcrState.Off or AcrState.Hold) return null!;
            var action = new PAction(ReaperSkill.勾刃, ActionType.Gcd, ActionTargetType.Target);
            return ReaperHelper.当前可执行(action) ? action : null!;
        });
    }
}
