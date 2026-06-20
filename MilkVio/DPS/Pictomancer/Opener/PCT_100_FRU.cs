using PromeRotation.Data;
using PromeRotation.Rotation;
using System.Collections.Generic;
using PromeRotation.Core;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Opener
{
    public class PCT_100_FRU : IOpener
    {
        public string OpenerName => "绘灵法师绝伊甸专用起手";

        // 起手
        public List<PAction> InCombatSequence => new()
        {
            // 1G
            new PAction(PCTSkill.神圣之白, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            new PAction(PCTSkill.动物构想, ActionType.OffGcd, ActionTargetType.Target),
            new PAction(PCTSkill.武器构想, ActionType.OffGcd, ActionTargetType.Self),
            
            // 2G
            new PAction(PCTSkill.翅膀彩绘, ActionType.Gcd, ActionTargetType.Self)
            {
                RequiresVerification = true
            },
            new PAction(PCTSkill.风景构想, ActionType.OffGcd, ActionTargetType.Self),
            
            // 3G
            new PAction(PCTSkill.重锤敲章1, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            new PAction(PCTSkill.减色混合, ActionType.OffGcd, ActionTargetType.Self),
            
            // 4G
            new PAction(PCTSkill.冰结之蓝青1, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            // 5G
            new PAction(PCTSkill.飞石之纯黄2, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            // 6G
            new PAction(PCTSkill.重锤掠刷2, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            new PAction(PCTSkill.翅膀构想, ActionType.OffGcd, ActionTargetType.Target),
            
            // 7G
            new PAction(PCTSkill.重锤抛光3, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            new PAction(PCTSkill.莫古力激流, ActionType.OffGcd, ActionTargetType.Target),
            
            // 8G
            new PAction(PCTSkill.天星棱光, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            // 9G
            new PAction(PCTSkill.闪雷之品红3, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            // 10G
            new PAction(PCTSkill.彗星之黑, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            // 11G
            new PAction(PCTSkill.彩虹点滴, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            new PAction(PCTSkill.即刻咏唱, ActionType.OffGcd, ActionTargetType.Self),
            
            // 13G
            new PAction(PCTSkill.神圣之白, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
            
            // 14G
            new PAction(PCTSkill.彩虹点滴, ActionType.Gcd, ActionTargetType.Target)
            {
                RequiresVerification = true
            },
        };
        
        // 倒计时 起手
        public void InitializeCountdown(CountDownHandler countdownHandler)
        {
            var 动物画 = new PAction(PCTSkill.动物彩绘, ActionType.Gcd, ActionTargetType.Self);
            var 武器画 = new PAction(PCTSkill.武器彩绘, ActionType.Gcd, ActionTargetType.Self);
            var 风景画 = new PAction(PCTSkill.风景彩绘, ActionType.Gcd, ActionTargetType.Self);
            var 彩虹点滴 = new PAction(PCTSkill.彩虹点滴, ActionType.Gcd, ActionTargetType.Target);
            if (!PctHelper.IsCreatureDrawn()) countdownHandler.AddAction(15000, 动物画);
            if (!PctHelper.IsCreatureDrawn()) countdownHandler.AddAction(13600, 武器画);
            if (!PctHelper.IsCreatureDrawn()) countdownHandler.AddAction(12100, 风景画);
            countdownHandler.AddAction(4500, 彩虹点滴);
        }
    }
}
