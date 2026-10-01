using PromeRotation.Data;
using PromeRotation.Rotation;
using MilkVio.DPS.Samurai.Level100;
using MilkVio.DPS.Samurai.Timeline;

namespace MilkVio.DPS.Samurai;

public class SamuraiRotationEventHandler : IRotationEventHandler
{
    private readonly SamuraiRotation _rotation;

    public SamuraiRotationEventHandler(SamuraiRotation rotation) => _rotation = rotation;

    public void OnUpdate()
    {
        _rotation.UpdatePlanning();
        _rotation.UpdatePrediction();
        _rotation.UpdateDebugLog();
    }

    public void OnOutOfBattleUpdate()
    {
    }

    public void OnBattleStarted()
    {
        _rotation.ResetPrediction();
        _rotation.UpdateDebugLog();
        _rotation.DebugLog.CombatStarted(Environment.TickCount64);
    }

    public void OnBattleUpdate()
    {
    }

    public void OnNoTarget()
    {
    }

    public void OnBattleEnded()
    {
        SamuraiTimeline.ResetSession("战斗结束");
        Samurai100Planning.Reset("战斗结束");
        _rotation.ResetPrediction();
        _rotation.DebugLog.CombatEnded(Environment.TickCount64, "战斗结束");
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
    }

    public void OnTerritoryChanged(ushort territoryId)
    {
        SamuraiTimeline.ResetSession("切换地图");
        Samurai100Planning.Reset("切换地图");
        _rotation.ResetPrediction();
        _rotation.DebugLog.CombatEnded(Environment.TickCount64, $"切换地图{territoryId}");
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
    }
}
