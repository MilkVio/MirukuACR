using MilkVio.DPS.Machinist.Planning;
using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.DPS.Machinist;

public class MachinistRotationEventHandler : IRotationEventHandler
{
    private readonly MachinistRotation _rotation;
    public MachinistRotationEventHandler(MachinistRotation rotation) => _rotation = rotation;
    public void OnUpdate()
    {
        MachinistHelper.UpdateWeaveLimit();
        _rotation.UpdatePlanning();
    }
    public void OnOutOfBattleUpdate() { }
    public void OnBattleStarted() => _rotation.DebugLog.CombatStarted(Environment.TickCount64);
    public void OnBattleUpdate() { }
    public void OnNoTarget() { }
    public void OnBattleEnded()
    {
        MachinistHelper.ResetWeaveLimit();
        MachinistPlanning.ResetSession("战斗结束");
        _rotation.DebugLog.CombatEnded(Environment.TickCount64, "战斗结束");
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
    }
    public void OnTerritoryChanged(ushort territoryId)
    {
        MachinistHelper.ResetWeaveLimit();
        MachinistPlanning.ResetSession("切换区域");
        _rotation.DebugLog.CombatEnded(Environment.TickCount64, $"切换区域{territoryId}");
    }
}
