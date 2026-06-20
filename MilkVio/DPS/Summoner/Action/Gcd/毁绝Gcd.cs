using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner.Action.Gcd;

public class 毁绝Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (me.DistanceToMe() > currentAttackRange) return new CheckResult(false, $"当前目标过远（>{currentAttackRange}m）");
        
        // 决策不允许的条件
        if (!me.HasStatus(SMNBuff.毁绝预备)) return new CheckResult(false, "当前没有毁绝预备Buff");
        if (SummonerHelper.IsBahamutAttuned()) return new CheckResult(false, "当前正在龙神召唤");
        if (PromeSettings.Instance.GetQt(SMNQt.龙神召唤))
        {
            if (SMNSkill.龙神召唤.GetActionCooldown() <= 1) return new CheckResult(false, "当前有龙神召唤");
        }

        if (PromeSettings.Instance.GetQt(SMNQt.倾泻资源))
        {
            if (JobGaugeHelper.SMN.SummonTimerRemaining == 0 && !JobGaugeHelper.SMN.IsIfritReady && !JobGaugeHelper.SMN.IsTitanReady && !JobGaugeHelper.SMN.IsGarudaReady)
            {
                return new CheckResult(false, "可以直接打下一个召唤物");
            }
            return new CheckResult(true, "倾泻资源");
        }
        
        if (me.HasStatus(SMNBuff.灼热之光) && me.Level < 90)
        {
            return new CheckResult(true, "有灼热之光 直接打");
        }
        
        if (StatusHelper.GetStatusLeftTime(me, SMNBuff.毁绝预备) < 7.5f)
        {
            return new CheckResult(true, "快他妈过期了");
        }
        
        if (SMNSkill.能量吸收.GetActionCooldown() < 7.5f)
        {
            return new CheckResult(true, "快他妈有能量吸收了");
        }
        
        if (SMNSkill.龙神召唤.GetActionCooldown() < 7.5f)
        {
            return new CheckResult(true, "快他妈有龙神了");
        }
        
        if (JobGaugeHelper.SMN.IsTitanAttuned)
        {
            return new CheckResult(false, "土神不需要这个辅助移动");
        }
        
        if (!SummonerHelper.IsGarudaDone(me) && JobGaugeHelper.SMN.AttunementCount == 0 && me.HasStatus(SMNBuff.螺旋气流预备) && MoveManager.IsLocalPlayerMoving)
        {
            return new CheckResult(true, "辅助移动");
        }
        
        if (!SummonerHelper.IsIfritDone(me) && JobGaugeHelper.SMN.AttunementCount != 0 && !me.HasStatus(SMNBuff.深红强袭预备) && !me.HasStatus(SMNBuff.深红旋风预备) && MoveManager.IsLocalPlayerMoving)
        {
            return new CheckResult(true, "辅助移动");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(SMNSkill.毁绝, ActionType.Gcd, ActionTargetType.Target);
    }
}
