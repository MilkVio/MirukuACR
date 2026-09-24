using MilkVio.DPS.Machinist.MCHData;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Rotation;

namespace MilkVio.DPS.Machinist.Opener;

public class MCH_100_Drill : IOpener
{
    public string OpenerName => "机工100通用钻头起手";

    // 全金属+五烈焰弹：末击距本G起点为2个普通G+6秒。
    // 给末击留0.50秒生效余量，给本G末尾留0.65秒动画锁+0.15秒余量，取可用区间中点。
    // WithWeaveDelay按真实GCD经过时间放行；300ms动画锁不会把野火提前。
    internal static int WildfireDelayMs(float gcd)
    {
        if (!float.IsFinite(gcd) || gcd <= 0) gcd = 2.5f;
        var earliest = Math.Max(0, 2 * gcd + 6 + .5f - 10);
        var latest = Math.Max(0, gcd - .65f - .15f);
        return (int)MathF.Round(Math.Min(latest, (earliest + latest) / 2) * 1000);
    }

    public List<PAction> InCombatSequence => new()
    {
        // 第1G钻头由倒计时预提交，开怪后接其两个能力技。
        new(MCHSkill.双将, ActionType.OffGcd, ActionTargetType.Target),
        new(MCHSkill.将死, ActionType.OffGcd, ActionTargetType.Target),

        // 第2～4G：产60电，在飞轮后召唤并准备第二个整备钻头。
        new(MCHSkill.空气锚, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.枪管加热, ActionType.OffGcd, ActionTargetType.Self),
        new(MCHSkill.回转飞锯, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.掘地飞轮, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.后式自走人偶, ActionType.OffGcd, ActionTargetType.Self),
        new(MCHSkill.整备, ActionType.OffGcd, ActionTargetType.Self),

        // 第5～6G：后段野火覆盖全金属和接下来的五发烈焰弹。
        new(MCHSkill.钻头, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.双将, ActionType.OffGcd, ActionTargetType.Target),
        new PAction(MCHSkill.野火, ActionType.OffGcd, ActionTargetType.Target)
            .WithWeaveDelay(WildfireDelayMs(ActionHelper.GetGcdTotal())),
        new(MCHSkill.全金属爆发, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.将死, ActionType.OffGcd, ActionTargetType.Target),
        new(MCHSkill.超荷, ActionType.OffGcd, ActionTargetType.Self),

        // 第7～11G：整轮短G单插，末发之后仍只插一次。
        new(MCHSkill.烈焰弹, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.双将, ActionType.OffGcd, ActionTargetType.Target),
        new(MCHSkill.烈焰弹, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.将死, ActionType.OffGcd, ActionTargetType.Target),
        new(MCHSkill.烈焰弹, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.双将, ActionType.OffGcd, ActionTargetType.Target),
        new(MCHSkill.烈焰弹, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.将死, ActionType.OffGcd, ActionTargetType.Target),
        new(MCHSkill.烈焰弹, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.双将, ActionType.OffGcd, ActionTargetType.Target),

        // 第12～15G：第三个钻头和完整123，然后交回普通求解。
        new(MCHSkill.钻头, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.将死, ActionType.OffGcd, ActionTargetType.Target),
        new(MCHSkill.双将, ActionType.OffGcd, ActionTargetType.Target),
        new(MCHSkill.热分裂弹1, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.将死, ActionType.OffGcd, ActionTargetType.Target),
        new(MCHSkill.热独头弹2, ActionType.Gcd, ActionTargetType.Target),
        new(MCHSkill.热狙击弹3, ActionType.Gcd, ActionTargetType.Target),
    };

    public void InitializeCountdown(CountDownHandler countdownHandler)
    {
        countdownHandler.AddAction(5000, new PAction(MCHSkill.整备, ActionType.OffGcd, ActionTargetType.Self));
        countdownHandler.AddAction(2000, () => new PAction(GameData.GetBestPotionId(), ActionType.Item, ActionTargetType.Self));
        countdownHandler.AddAction(400, new PAction(MCHSkill.钻头, ActionType.Gcd, ActionTargetType.Target));
    }
}
