using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Dancer.DNCData;
namespace MilkVio.DPS.Dancer.Action.Gcd;

public class 小舞和结束动作释放Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var player = Core.Me;
        var 小舞CD = DNCSkill.标准舞步.GetActionCooldown();
        var 百花CD = DNCSkill.百花.GetActionCooldown();
        
        if (player == null) return new CheckResult(false, "我不存在");
        var gcd和目标距离 = GameData.GetCurrentAttackRange(25f);
        
        if (PromeSettings.Instance.GetQt(DNCQt.不打小舞)) return new CheckResult(false, "不打小舞");
        
        // 强制小舞
        if ((PromeSettings.Instance.GetQt(DNCQt.强制小舞) || PromeSettings.Instance.GetQt(DNCQt.强制小舞和打出)) && 小舞CD <= 1) 
        {
            return new CheckResult(true, "强制小舞");
        }
        
        // 120中优先结束动作 
        if (!PromeSettings.Instance.GetQt(DNCQt.不打120) && DNCSkill.技巧舞步.GetActionCooldown() >= 110 && player.Level >= 96)
        {
            if (百花CD >= 0 && 百花CD <= 5)
            {
                return new CheckResult(false, "等待百花触发结束动作打团辅");
            } 
        }
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        if (Core.Me.HasStatus(DNCBuff.技巧舞步结束) && DNCSkill.探戈.GetActionCooldown() == 0)
        {
            return new CheckResult(false, "该死的探戈没放出去呢");
        }

        if (PromeSettings.Instance.GetQt(DNCQt.不打120))
        {
            if (小舞CD <= 1)
            {
                if (Core.Me.DistanceToMe() <= gcd和目标距离)
                {
                    return new CheckResult(true, "距离 <= 25 开启不打120");
                }
            }   
        }

        if (小舞CD <= 1)
        {
            return new CheckResult(true, "距离 <= 25");
        }
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        var bf = Core.Me;
        if (bf.HasStatus(DNCBuff.结束动作预备)) return new PAction(DNCSkill.结束动作, ActionType.Gcd, ActionTargetType.Self);
            
        return new PAction(DNCSkill.标准舞步, ActionType.Gcd, ActionTargetType.Self);
    } 
}
