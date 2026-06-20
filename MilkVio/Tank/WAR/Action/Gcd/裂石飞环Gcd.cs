using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.Tank.WAR.WARData;

namespace MilkVio.Tank.WAR.Action.Gcd;

public class 裂石飞环Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        // Qt控制
        if (PromeSettings.Instance.GetQt(WARQt.攒资源)) return new CheckResult(false, "当前正在攒资源");
        
        if (Core.Me.DistanceToMe() <= currentMeleeRange)
        {
            
            if (PromeSettings.Instance.GetQt(WARQt.优先续红斩) && !Core.Me.HasStatus(WARBuff.战场风暴) && !PromeSettings.Instance.GetQt(WARQt.最终爆发))
            {
                if (JobGaugeHelper.WAR.Beast == 100) return new CheckResult(true, $"溢出");
                return new CheckResult(false, "续红斩");
            }
            
            // 原初的解放逻辑
            if (Core.Me.GetStatusStackCount(WARBuff.原初的解放) >= 1)
            {
                return new CheckResult(true, $"距离 < {currentMeleeRange}");
            }
            
            if(JobGaugeHelper.WAR.Beast >= 50 && PromeSettings.Instance.GetQt(WARQt.最终爆发)) return new CheckResult(true, $"距离 < {currentMeleeRange}");
            if(JobGaugeHelper.WAR.Beast >= 50 && Core.Me.HasStatus(WARBuff.原初的觉悟)) return new CheckResult(true, $"距离 < {currentMeleeRange} 爆发中");
            
            if(JobGaugeHelper.WAR.Beast >= 80) return new CheckResult(true, $"距离 < {currentMeleeRange}");
            
            // 这里有一个如果解放好了 兽魂还 < 20s有下一层，还有50以上兽魂？ 算一下解放三个 50兽魂一个 一共20s吧
            if (PromeSettings.Instance.GetQt(WARQt.不溢出战嚎))
            {
                if (PromeSettings.Instance.GetQt(WARQt.不溢出战嚎))
                {
                    if (WARSkill.战嚎.GetActionCooldown() <= 20 && JobGaugeHelper.WAR.Beast >= 50)
                    {
                        return new CheckResult(false, $"打一个裂石飞环");
                    }
                }
            }
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(WARSkill.裂石飞环, ActionType.Gcd, ActionTargetType.Target);
    }
}
