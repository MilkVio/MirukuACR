using PromeRotation.Data;
using PromeRotation.Rotation;

namespace MilkVio.Healer.Common;

public sealed class DefaultRotationEventHandler : IRotationEventHandler
{
    public void OnUpdate()
    {
    }

    public void OnOutOfBattleUpdate()
    {
    }

    public void OnBattleStarted()
    {
        HealerUtils.MarkBattleStarted();
    }

    public void OnBattleUpdate()
    {
    }

    public void OnNoTarget()
    {
    }

    public void OnBattleEnded()
    {
        HealerUtils.MarkBattleEnded();
        PromeSettings.Instance.OpenerHasBeenExecuted = false;
    }

    public void OnTerritoryChanged(ushort territoryId)
    {
    }
}
