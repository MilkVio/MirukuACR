using ECommons.DalamudServices;
using PromeRotation.Data;
using PromeRotation.Rotation;
using MilkVio.Tank.GNB.GNBData;


namespace MilkVio.Tank.GNB
{
    public class GunbreakerRotationEventHandler : IRotationEventHandler
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
            Svc.Log.Info("[GNB EventHandler] 战斗开始！");
            
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
        // 交战状态变为False的时候会执行一次下面的代码
        public void OnBattleEnded()
        {
            // 重置战斗数据
            GNBBattleData.Instance = new GNBBattleData();
            // 考虑把这玩意扔到rotationmanager里面，也不能所有职业都搞一个这个
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
        }

        // 切换区域会执行一次下面的方法
        public void OnTerritoryChanged(ushort territoryId)
        {
            
        }
    }
}
