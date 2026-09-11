using PromeRotation.Data;
using PromeRotation.Rotation;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper
{
    public class ReaperRotationEventHandler : IRotationEventHandler
    {
        public void OnUpdate()
        {
            ReaperBattleData.Instance.SynchronizeLevel();
            ReaperBattleData.Instance.AutoSoulsow.Tick();
            ReaperBattleData.Instance.DmuOpener.Tick();
            ReaperRotation.UpdatePlanner();
            ReaperRotation.UpdateDebugLog();
        }

        public void OnOutOfBattleUpdate()
        {

        }

        public void OnBattleStarted()
        {
            ReaperBattleData.Instance.AutoSoulsow.Cancel();
            ReaperBattleData.Instance.DebugLog.CombatStarted(Environment.TickCount64);
            ReaperRotation.UpdateDebugLog();
        }

        public void OnBattleUpdate()
        {

        }

        // 战斗中没有可攻击目标时每帧都会执行一次下面的方法
        public void OnNoTarget()
        {
        }

        // 交战状态变为False的时候会执行一次下面的代码

        public void OnBattleEnded()
        {
            ReaperBattleData.Instance.DebugLog.CombatEnded(Environment.TickCount64, "战斗结束");
            ReaperBattleData.Instance.Reset();
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
        }

        public void OnTerritoryChanged(ushort territoryId)
        {
            ReaperBattleData.Instance.DebugLog.CombatEnded(Environment.TickCount64, $"切换地图{territoryId}");
            ReaperBattleData.Instance.Reset();
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
        }
    }
}
