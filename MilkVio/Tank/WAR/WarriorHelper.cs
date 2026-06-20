using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.Tank.WAR.WARData;

namespace MilkVio.Tank.WAR;

public static class WarriorHelper
{
    public static PAction ShouldExecuteRedBuff()
    {
        // 这里可以近似认为几种情况
        // 没有红斩 直接续
        // 解放>=10s 红斩 < 30s 直接续
        // 解放<10s 20s < 红斩 直接续
        // 解放<10s 20s < 红斩 < 30s 不续
        // 可能存在 不打解放 那就是解放<10s 红斩 < 30s直接续
        // 可能还有一个最终爆发 只打绿斩 只打红斩
        
        var player = Core.Me;
        var redBuffLeftTime = player.GetStatusLeftTime(WARBuff.战场风暴);
        var libCd = WARSkill.原初的解放.GetActionCooldown();
        // 没有红斩 直接续
        if (!player.HasStatus(WARBuff.战场风暴))
        {
            return new PAction(WARSkill.暴风碎, ActionType.Gcd, ActionTargetType.Target);
        }
        
        // 解放>=10s 红斩 < 30s 直接续
        if (redBuffLeftTime <= 30 && libCd >= 10)
        {
            return new PAction(WARSkill.暴风碎, ActionType.Gcd, ActionTargetType.Target);
        }
        
        // 解放<10s 20s < 红斩 直接续
        if (redBuffLeftTime <= 20 && libCd <= 10)
        {
            return new PAction(WARSkill.暴风碎, ActionType.Gcd, ActionTargetType.Target);
        }

        if (PromeSettings.Instance.GetQt(WARQt.不打60) && 30 <= redBuffLeftTime)
        {
            return new PAction(WARSkill.暴风碎, ActionType.Gcd, ActionTargetType.Target);
        }
        
        // 解放<10s 20s < 红斩 < 30s 不续
        if (libCd <= 10 &&
            (20 <= redBuffLeftTime && redBuffLeftTime <= 30))
        {
            return new PAction(WARSkill.暴风斩, ActionType.Gcd, ActionTargetType.Target);
        }
        
        return new PAction(WARSkill.暴风斩, ActionType.Gcd, ActionTargetType.Target);
    }

    public static PAction GetBasePAction()
    {
        var lastComboId = ActionHelper.GetLastComboID();
        
        if (PromeSettings.Instance.GetQt(WARQt.只打绿斩))
        {
            if(lastComboId == WARSkill.重劈) return new PAction(WARSkill.凶残裂, ActionType.Gcd, ActionTargetType.Target);
            if (lastComboId == WARSkill.凶残裂) return new PAction(WARSkill.暴风斩, ActionType.Gcd, ActionTargetType.Target);
            return new PAction(WARSkill.重劈, ActionType.Gcd, ActionTargetType.Target);
        }
        
        if (PromeSettings.Instance.GetQt(WARQt.只打红斩))
        {
            if(lastComboId == WARSkill.重劈) return new PAction(WARSkill.凶残裂, ActionType.Gcd, ActionTargetType.Target);
            if (lastComboId == WARSkill.凶残裂) return new PAction(WARSkill.暴风碎, ActionType.Gcd, ActionTargetType.Target);
            return new PAction(WARSkill.重劈, ActionType.Gcd, ActionTargetType.Target);
        }
        
        if(lastComboId == WARSkill.重劈) return new PAction(WARSkill.凶残裂, ActionType.Gcd, ActionTargetType.Target);
        if (lastComboId == WARSkill.凶残裂) return ShouldExecuteRedBuff();
        return new PAction(WARSkill.重劈, ActionType.Gcd, ActionTargetType.Target);
    }
    
    public static PAction? AutoGuard(bool mode)
    {
        var player = Core.Me;
        if (player == null) return null;
        
        if (mode)
        {
            if (player.HasStatus(WARBuff.守护))
            {
                return null;
            }
            if (!player.HasStatus(WARBuff.守护))
            {
                return new PAction(WARSkill.守护, ActionType.OffGcd, ActionTargetType.Self);
            }
        }

        if (mode == false)
        {
            if (player.HasStatus(WARBuff.守护))
            {
                return new PAction(WARSkill.守护, ActionType.OffGcd, ActionTargetType.Self);
            }
        }
        return null;
    }
}
