using MilkVio.DPS.Reaper.ReaperData;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.DPS.Reaper.Opener;

public class RPR_90_Standard : IOpener
{
    public string OpenerName => "镰刀90级标准起手";
    public List<PAction> InCombatSequence
    {
        get
        {
            ReaperBattleData.Instance.SynchronizeLevel();
            ReaperBattleData.Instance.Planner90.BeginOpener(ReaperRotation.ReadState());
            return new();
        }
    }
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var at = (int)MathF.Round(ReaperSettings.Instance.勾刃倒数预读时间 * 1000);
        countdownHandler.AddAction(at, () =>
        {
            if (!ReaperLevelRules.UsesLevel90(Core.Me?.Level ?? 0) || !ReaperSettings.Instance.启用起手
                || PromeSettings.Instance.EnableAcr is AcrState.Off or AcrState.Hold) return null!;
            var action = new PAction(ReaperSkill.勾刃, ActionType.Gcd, ActionTargetType.Target);
            return ReaperHelper.当前可执行(action) ? action : null!;
        });
    }
}
