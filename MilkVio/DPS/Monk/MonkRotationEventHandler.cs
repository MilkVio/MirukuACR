using System.Numerics;
using ECommons.DalamudServices;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Rotation;
using MilkVio.DPS.Monk.MNKData;

namespace MilkVio.DPS.Monk
{
    public class MonkRotationEventHandler : IRotationEventHandler
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
            // 强制对齐爆发控制
            if (PromeSettings.Instance.GetQt(MNKQt.强制对齐爆发))
            {
                var 团辅 = MNKSkill.义结金兰.GetActionCooldown();
                var 猛枪 = MNKSkill.红莲极意.GetActionCooldown();
                
                if (团辅 < 10 && (猛枪 > 0 && 猛枪 < 35))
                {
                    if (!PromeSettings.Instance.GetQt(MNKQt.不打60) || !PromeSettings.Instance.GetQt(MNKQt.不打120))
                    {
                        PromeSettings.Instance.SetQt(MNKQt.不打60, true);
                        PromeSettings.Instance.SetQt(MNKQt.不打120, true);
                    }
                }
                
                if (猛枪 < 10)
                {
                    if (团辅 > 50 && 团辅 < 72.5)
                    {
                        return;
                    }

                    if (团辅 > 0 && 团辅 < 50)
                    {
                        if (!PromeSettings.Instance.GetQt(MNKQt.不打60) || !PromeSettings.Instance.GetQt(MNKQt.不打120))
                        {
                            PromeSettings.Instance.SetQt(MNKQt.不打60, true);
                            PromeSettings.Instance.SetQt(MNKQt.不打120, true);
                        }
                    }
                }

                if (团辅 == 0 && 猛枪 == 0)
                {
                    var isCanOpen = ActionHelper.GetGcdRemain() < 0.1f;
                    if (isCanOpen)
                    {
                        if (PromeSettings.Instance.GetQt(MNKQt.不打60) || PromeSettings.Instance.GetQt(MNKQt.不打120) )
                        {
                            PromeSettings.Instance.SetQt(MNKQt.不打60, false);
                            PromeSettings.Instance.SetQt(MNKQt.不打120, false);
                        }
                    }
                }
            }
            
            var target = Core.Target;
            if (target == null) return;

            if (!PromeSettings.Instance.GetQt(MNKQt.TP身位))
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
            bool hasCoe = Core.Me.HasStatus(MNKBuff.猛豹身形);
            bool hasPb  = Core.Me.HasStatus(MNKBuff.震脚);
            Positional required = Positional.None;
            
            // 没有震脚
            if (!hasPb && hasCoe)
            {
                required = JobGaugeHelper.MNK.CoeurlFury > 0 ? Positional.Flank : Positional.Rear;
            }
            
            if (hasPb && MonkHelper.GetNextNadi() == NadiType.阳)
            {
                if (MonkHelper.AutoGetSolarBeastType() == BeastType.Coe)
                {
                    required = JobGaugeHelper.MNK.CoeurlFury > 0 ? Positional.Flank : Positional.Rear;
                }
            }

            return required;
        }
        
        // 战斗中没有可攻击目标时每帧都会执行一次下面的方法
        public void OnNoTarget()
        {
        }

        // 交战状态变为False的时候会执行一次下面的代码
        public void OnBattleEnded()
        {
            tpReady = true;
            PromeSettings.Instance.OpenerHasBeenExecuted = false;
            MonkHelper.RestQt();
        }

        // 切换区域会执行一次下面的方法
        public void OnTerritoryChanged(ushort territoryId)
        {
            
        }
    }
}
