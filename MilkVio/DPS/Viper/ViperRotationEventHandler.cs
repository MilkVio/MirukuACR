using System.Numerics;
using Dalamud.Game.ClientState.JobGauge.Enums;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Rotation;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper
{
    public class ViperRotationEventHandler : IRotationEventHandler
    {
        private bool tpReady = true;
        public void OnUpdate()
        {

        }

        public void OnOutOfBattleUpdate()
        {

        }

        public void OnBattleStarted()
        {

        }

        public void OnBattleUpdate()
        {
            var target = Core.Target;
            if (target == null) return;
            
            if (!PromeSettings.Instance.GetQt(ViperQt.TP身位))
                return;
            
            // 先判断是否需要 TP 身位
            Positional need = GetNeededStance();
            if (need == Positional.None)
                return;

            Positional my = TargetHelper.GetTargetPositional();
            if (my == need)
                return;

            if (Core.Me.HasStatus(1250))
                return;

            if (!TargetHelper.HasPositionalRequirement(target))
            {
                return;
            }
            
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

        // 战斗中没有可攻击目标时每帧都会执行一次下面的方法
        public void OnNoTarget()
        {
        }

        // 交战状态变为False的时候会执行一次下面的代码

        public void OnBattleEnded()
        {
            ViperBattleData.Instance.Reset();
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
            tpReady = true;
        }

        public void OnTerritoryChanged(ushort territoryId)
        {
            ViperBattleData.Instance.Reset();
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
            tpReady = true;
        }

        private Positional GetNeededStance()
        {
            var me = Core.Me;
            var myPostional = TargetHelper.GetTargetPositional();
            var needPostional = Positional.None;
            if (ViperHelper.IsIn强碎灵())
            {
                // 优先续buff 身位不匹配交给真北/TP身位
                if (me.GetStatusLeftTime(ViperBuff.急速) < 3)
                    needPostional = Positional.Rear;

                if (me.GetStatusLeftTime(ViperBuff.猛袭) < 3)
                    needPostional = Positional.Flank;

                if (JobGaugeHelper.VPR.蛇剑连状态 == DreadCombo.Dreadwinder)
                {
                    needPostional = myPostional;
                    if (myPostional == Positional.Front)
                    {
                        needPostional = Positional.Rear;
                    }
                }
                else if (JobGaugeHelper.VPR.蛇剑连状态 == DreadCombo.SwiftskinsCoil)
                {
                    needPostional = Positional.Flank;
                }
                else
                {
                    needPostional = Positional.Rear;
                }
            }
            else // 在普通攻击逻辑中
            {
                var nextActionId = RotationManager.GetCurrentRotation().NextGcd().ActionId;
                if(nextActionId == ViperSkill.侧击獠齿左3绿 || nextActionId == ViperSkill.侧裂獠齿右3绿)
                    needPostional = Positional.Flank;
            
                if(nextActionId == ViperSkill.侧击獠齿左3红 || nextActionId == ViperSkill.侧裂獠齿右3红)
                    needPostional = Positional.Rear;
            }

            return needPostional;
        }
    }
}
