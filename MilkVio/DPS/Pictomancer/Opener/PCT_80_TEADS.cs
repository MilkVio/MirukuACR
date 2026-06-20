using PromeRotation.Data;
using PromeRotation.Rotation;
using System.Collections.Generic;
using PromeRotation.Core;
using MilkVio.DPS.Pictomancer.PCTData;

namespace MilkVio.DPS.Pictomancer.Opener
{
    public class PCT_80_TEADS : IOpener
    {
        public string OpenerName => "绘灵法师绝亚DollSkip起手";

        // 起手
        public List<PAction> InCombatSequence => new()
        {
            // 1G
            new PAction(PCTSkill.翅膀彩绘, ActionType.Gcd, ActionTargetType.Self)
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
            var 火炎1 = new PAction(PCTSkill.火炎之红1, ActionType.Gcd, ActionTargetType.Target);
            var 绒球 = new PAction(PCTSkill.动物构想, ActionType.Gcd, ActionTargetType.Target);
            if (!PctHelper.IsCreatureDrawn()) countdownHandler.AddAction(15000, 动物画);
            if (!PctHelper.IsCreatureDrawn()) countdownHandler.AddAction(13600, 武器画);
            if (!PctHelper.IsCreatureDrawn()) countdownHandler.AddAction(12100, 风景画);
            countdownHandler.AddAction(2000, 火炎1);
            countdownHandler.AddAction(500, 绒球);
        }
    }
}
