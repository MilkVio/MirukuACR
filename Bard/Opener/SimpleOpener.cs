using Bard.Data;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Rotation;

namespace Bard.Opener;

public class SimpleOpener : IOpener
{
    public string OpenerName => "诗人示例起手";

    // 起手序列
    public List<PAction> InCombatSequence => new()
    {
        // 1G
        new PAction(BardSkill.直线射击, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        
        // 2G
        new PAction(BardSkill.直线射击, ActionType.Gcd, ActionTargetType.Target)
        {
            RequiresVerification = true
        },
        new PAction(BardSkill.战斗之声, ActionType.OffGcd, ActionTargetType.Self)
    };
        
    // 倒计时 起手
    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        var 直线射击 = new PAction(BardSkill.直线射击, ActionType.OffGcd, ActionTargetType.Target);
        countdownHandler.AddAction(5000, () => 测试技能());
        countdownHandler.AddAction(300, 直线射击);
    }
    
    // 这是一个测试技能，用于构造一个实时求解，需要在起手阶段判断的技能
    private static PAction? 测试技能()
    {
        if (Core.Me.HasStatus(123)) return new PAction(BardSkill.直线射击, ActionType.OffGcd, ActionTargetType.Target);
        return null;
    }
}