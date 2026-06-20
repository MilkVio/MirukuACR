using ECommons.DalamudServices;
using PromeRotation.Data;
using PromeRotation.Rotation;

using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK
{
    public class DarkKnightRotationEventHandler : IRotationEventHandler
    {
        // 每帧都会执行一次下面的方法
        public void OnUpdate()
        {
            
        }

        // 非战斗状态下每帧都会执行一次下面的方法
        public void OnOutOfBattleUpdate()
        {
            
        }

        // 交战状态变为True的时候会执行一次下面的代码
        public void OnBattleStarted()
        {
            
        }
        
        // 这里战斗中每帧都会执行一次下面的方法
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
            DRKBattleData.Instance = new DRKBattleData();
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
            DarkKnightHelper.RestQT();
        }

        // 切换区域会执行一次下面的方法
        public void OnTerritoryChanged(ushort territoryId)
        {
            DarkKnightHelper.RestQT();
        }
    }
}
