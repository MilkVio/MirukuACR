using Dalamud.Game.ClientState.JobGauge.Enums;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Ninja.NinjaData;
using MilkVio.DPS.Viper.ViperData;

namespace MilkVio.DPS.Viper.Action.OffGcd;

public class 真北OffGcd : IDecisionResolver
{
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (!PromeSettings.Instance.GetQt(ViperQt.真北)) return new CheckResult(false, "未开启自动真北");
        // 前置条件
        if(Core.Me.HasStatus(1250)) return new CheckResult(false, "自身已存在真北");

        if (ViperHelper.IsIn附体()) return new CheckResult(false, "当前在附体状态中");

        var me = Core.Me;
        var myPostional = TargetHelper.GetTargetPositional();
        var needPostional = Positional.None;
        
        // 在强碎灵逻辑中
        if (ViperHelper.IsIn强碎灵())
        {
            if (JobGaugeHelper.VPR.蛇剑连状态 == DreadCombo.Dreadwinder)
            {
                needPostional = myPostional;
                if (myPostional == Positional.Front)
                {
                    needPostional = Positional.Rear;
                }
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
        
        if (needPostional != Positional.None && UniversalData.MeleeUniversalSkill.真北.GetActionCharges() >= 1)
        {
            if (needPostional != myPostional && ActionHelper.GetGcdRemain() < 1f)
            {
                return new CheckResult(true, "打个身位");
            }
        }
        return new CheckResult(false, "不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(UniversalData.MeleeUniversalSkill.真北, ActionType.OffGcd, ActionTargetType.Self);
    }
}
