using System.Numerics;
using ECommons.DalamudServices;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Rotation;
using MilkVio.DPS.Ninja;
using MilkVio.DPS.Ninja.NinjaData;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai
{
    public class SamuraiRotationEventHandler : IRotationEventHandler
    {
        private bool tpReady = true;
        
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
            var target = Core.Target;
            if (target == null) return;

            if (!PromeSettings.Instance.GetQt(SAMQt.TP身位))
                return;
            
            if (SamuraiHelper.GetBestJuhe() != 居合类型.无)
            {
                return;
            }
            
            if (Core.Me.IsCasting)
            {
                return;
            }

            if (Core.Me.HasStatus(SAMBuff.奥义浪斩预备))
            {
                return;
            }
            
            // 先判断是否需要 TP 身位
            Positional need = GetNeededStance();
            if (need == Positional.None)
                return;

            Positional my = TargetHelper.GetTargetPositional();
            if (my == need)
                return;

            if (Core.Me.HasStatus(1250))
                return;

            // 获取GCDRemain
            float remain = ActionHelper.GetGcdRemain();

            // 新GCD开始 → 重置可TP
            if (remain > 1.0f)
            {
                tpReady = true;
                return;
            }

            // 进入GCD结尾（<0.05），且标志位允许 → 执行TP，然后锁定
            if (tpReady && remain < 0.03f)
            {
                Vector3 posA = TargetHelper.GetStancePoint(target, need);
                Vector3 posB = Core.Me.Position;
                // Svc.Chat.PrintError($"{posA} {posB}");
                _ = HackHelper.TeleportWithReturn(posA, posB, 80);
                
                tpReady = false;
            }
        }
        
        private static Positional GetNeededStance()
        {
            return SamuraiHelper.GetNeedPositional();
        }
        
        // 战斗中没有可攻击目标时每帧都会执行一次下面的方法
        public void OnNoTarget()
        {
        }

        // 交战状态变为False的时候会执行一次下面的代码

        public void OnBattleEnded()
        {
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
        }

        // 切换区域会执行一次下面的方法
        public void OnTerritoryChanged(ushort territoryId)
        {
            
        }
    }
}
