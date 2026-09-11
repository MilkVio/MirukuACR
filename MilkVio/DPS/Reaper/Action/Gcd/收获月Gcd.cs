using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper.Action.Gcd;

public class 收获月Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (!PromeSettings.Instance.GetQt(ReaperQt.收获月)) return new CheckResult(false, "未开启收获月");

        var me = Core.Me;
        if (me == null) return new CheckResult(false, "自身未加载");
        if (!me.HasStatus(ReaperBuff.播魂种Buff)) return new CheckResult(false, "没有播魂种");
        if (ReaperHelper.IsIn妖异之镰()) return new CheckResult(false, "先消费妖异之镰/处刑");

        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");

        if (me.DistanceToMe() > GameData.GetCurrentAttackRange(25)) return new CheckResult(false, "当前目标过远（>25m）");

        var state = ReaperRotation.ReadState();
        if (!ReaperResources.AllowsHarvestMoon(state)) return new CheckResult(false, "保留收获月或附体收尾时间");
        if (state.Enshrouded > 0) return new CheckResult(true, "附体远离/移动或窗口末尾使用收获月");

        // 远离近战距离 直接使用
        if (me.DistanceToMe() > currentMeleeRange) return new CheckResult(true, "远离近战距离 收获月止损");

        if (ReaperBattleData.Instance.Window.Active)
            return new CheckResult(ReaperBattleData.Instance.ActivePlanner.GcdAction == ReaperSkill.收获月, "按输出窗口安排收获月");
        if (!PromeSettings.Instance.GetQt(ReaperQt.倾泻资源)) return new CheckResult(false, "近战距离内 未开启倾泻资源 留着收获月");

        return new CheckResult(ReaperBattleData.Instance.ActivePlanner.GcdAction == ReaperSkill.收获月, "按倾泻规划使用收获月");
    }

    public PAction GetAction()
    {
        return new PAction(ReaperSkill.收获月, ActionType.Gcd, ActionTargetType.Target);
    }
}
