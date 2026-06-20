using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using MilkVio.Tank.PLD.PLDData;

namespace MilkVio.Tank.PLD.Action.Gcd;

public class 圣灵Gcd : IDecisionResolver
{
    public CheckResult Check()
    {
        var currentMeleeRange = GameData.GetCurrentMeleeRange();
        var currentAttackRange = GameData.GetCurrentAttackRange(25);
        
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");
        if (Core.Target.IsPlayer()) return new CheckResult(false, "当前目标为玩家");
        
        if (Core.Me.DistanceToMe() > currentAttackRange) return new CheckResult(false, "当前目标距离过远");
        if (Core.Me.CurrentMp < 1000) return new CheckResult(false, "蓝量不够");
        var me = Core.Me;
        var level = me.Level;
        var is76CanUse = me.HasStatus(PLDBuff.祈告剑预备) || me.HasStatus(PLDBuff.赎罪剑预备) || me.HasStatus(PLDBuff.葬送剑预备);
        var hasFof = me.HasStatus(PLDBuff.战逃反应buff);
        var lastComboId = ActionHelper.GetLastComboID();
        var fofLeftTime = me.GetStatusLeftTime(PLDBuff.战逃反应buff);
        
        if (PromeSettings.Instance.GetQt(PLDQt.硬读圣灵) && !MoveManager.IsLocalPlayerMoving)
        {
            if (!is76CanUse && !me.HasStatus(PLDBuff.神圣魔法效果提高))
            {
                if (ActionHelper.GetComboLeftTime() == 0 || lastComboId == PLDSkill.先锋剑)
                {
                    return new CheckResult(true, "硬读圣灵");
                }
            }
        }

        if (me.Level < 90 && me.HasStatus(PLDBuff.安魂祈祷))
        {
            return new CheckResult(true, "90级以下安魂圣灵");
        }
        
        if (Core.Me.HasStatus(PLDBuff.神圣魔法效果提高))
        {
            // 过期
            if (StatusHelper.GetStatusLeftTime(me, PLDBuff.神圣魔法效果提高) < 2.5f)
            {
                return new CheckResult(true, "快过期了");
            }

            if (PromeSettings.Instance.GetQt(PLDQt.最优战逃))
            {
                if (fofLeftTime < 3 && fofLeftTime != 0 && me.HasStatus(PLDBuff.赎罪剑预备))
                {
                    return new CheckResult(true, "战逃需要打一个");
                }
            }
            
            // 倾泻QT
            if (PromeSettings.Instance.GetQt(PLDQt.倾泻资源))
            {
                return new CheckResult(true, "快过期了");
            }
            
            if (PromeSettings.Instance.GetQt(PLDQt.远离圣灵))
            {
                // 以能否近战距离作为区分
                // 在近战距离的话按照留圣灵逻辑来
                // 在近战距离外就打一个
                if (Core.Me.DistanceToMe() <= currentMeleeRange)
                {
                    if (ActionHelper.GetLastComboID() == PLDSkill.暴乱剑)
                    {
                        // 这里以76级前后区分 确定是否学会了
                        if (level < 76)
                        {
                            return new CheckResult(true, "没有强化三连");
                        }
                        if (!is76CanUse)
                        {
                            return new CheckResult(true, "强化三连都打完");
                        }
                    }

                    if (hasFof)
                    {
                        if (!is76CanUse)
                        {
                            return new CheckResult(true, "战逃内强化三连都打完");
                        }
                    }
                }

                if (Core.Me.DistanceToMe() > currentMeleeRange)
                {
                    return new CheckResult(true, "距离 < 3");
                }
            }
            else if (!PromeSettings.Instance.GetQt(PLDQt.远离圣灵)) 
            {
                return new CheckResult(true, "距离 < 3");
            }
        }
        
        
        return new CheckResult(false, "当前不满足任何条件");
    }

    public PAction GetAction()
    {
        return new PAction(PLDSkill.圣灵, ActionType.Gcd, ActionTargetType.Target);
    }
}
