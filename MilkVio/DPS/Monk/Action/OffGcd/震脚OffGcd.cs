using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Resolvers;
using MilkVio.DPS.Monk.MNKData;

namespace MilkVio.DPS.Monk.Action.OffGcd;

public class 震脚OffGcd : IDecisionResolver
{
    // todo
    public CheckResult Check()
    {
        if (Core.Target == null) return new CheckResult(false, "当前无目标");
        if (Core.Target.EntityId == Core.Me.EntityId) return new CheckResult(false, "当前目标为自己");

        var brotherHood = false;
        if (Core.Me.Level == 100 && !PromeSettings.Instance.GetQt(MNKQt.不打120))
        {
            if (MNKSkill.义结金兰.GetActionCooldown() < 5)
            {
                brotherHood = true;
            }
        }
        else if (Core.Me.Level < 100 && !PromeSettings.Instance.GetQt(MNKQt.不打120))
        {
            if (MNKSkill.义结金兰.GetActionCooldown() < 3)
            {
                brotherHood = true;
            }
        }
        
        // QT控制
        if (PromeSettings.Instance.GetQt(MNKQt.攒资源)) return new CheckResult(false, "已开启攒资源");
        if (PromeSettings.Instance.GetQt(MNKQt.不打震脚)) return new CheckResult(false, "已开启不打震脚");
        
        // 肯定不可以打震脚自身条件 有无演武放在下面判断
        if (Core.Me.HasStatus(MNKBuff.震脚)) return new CheckResult(false, "自身已有震脚");
        if (JobGaugeHelper.MNK.BlitzTimeRemaining > 0)  return new CheckResult(false, "自身已有必杀技");
        
        
        // 可以打震脚的条件
        // 这里粗略认为 震脚只能打在红莲 义结金兰内；但是震脚GCD是检测有无震脚的，所以这个应该没问题
        // 例如需要凭空搓震脚的情况，在AOE震脚GCD中，加入QT控制 —— 无目标震脚，没有目标的时候也能打震脚；这里是否要考虑检测场上没有任何一个可以选中的单位呢？
        
        if (MonkHelper.GetRealPbCharge() >= 1)
        {
            // 这里先从最终爆发开始区分
            if (!PromeSettings.Instance.GetQt(MNKQt.最终爆发))
            {
                // 常规逻辑下 什么时候能打震脚
                // 大体分为两种情况
                // 1.红莲好了（自身有红莲），义结金兰没好（60秒）
                // 不提前打震脚
                // 通过检测红莲buff剩余CD来确定 当前有魔猿的话是否要打出去魔猿来拿1
                // 2.红莲没好 && 义结金兰
                // 7秒？的时候开阵脚 （提前打掉
                // 理论上团辅内可以打掉所有震脚，先这样写
                // 3.震脚==2 红莲还剩7秒 必杀技可以打到红莲内
                // 特殊情况
                // 震脚两层满了 并且红莲7秒内转不好 不考虑，轴控解决
                
                // 1情况 60秒
                if ((Core.Me.HasStatus(MNKBuff.红莲极意) || MNKSkill.红莲极意.GetActionCooldown() < 7) && MNKSkill.义结金兰.GetActionCooldown() > 10 && MonkHelper.GetRealPbCharge() > 1.5 && !PromeSettings.Instance.GetQt(MNKQt.不打60))
                {
                    // 情况3 震脚==2 红莲还剩7秒 必杀技可以打到红莲内
                    if (MonkHelper.GetRealPbCharge() == 2f  && MNKSkill.红莲极意.GetActionCooldown() < 7)
                    {
                        return new CheckResult(true, "60红莲内即将溢出 打一个");
                    }
                    
                    // 判断红莲buff时间决策演武震脚
                    if (Core.Me.HasStatus(MNKBuff.红莲极意))
                    {
                        if (Core.Me.HasStatus(MNKBuff.演武) || Core.Me.HasStatus(MNKBuff.魔猿身形))
                        {
                            // 未来如果打不出一组完整的必杀技，返回一个震脚
                            if(Core.Me.GetStatusLeftTime(MNKBuff.红莲极意) <= 8) return new CheckResult(true, "必杀技即将溢出 直接打出");
                            return new CheckResult(false, "有演武 先打一个演武");
                        }
                        
                        return new CheckResult(true, "红莲内可以打一个震脚");
                    }
                }
                
                // 2情况 团辅（即将）转好了 同时 必须为
                if ((brotherHood && !PromeSettings.Instance.GetQt(MNKQt.不打120)) || Core.Me.HasStatus(MNKBuff.义结金兰))
                {
                    if (Core.Me.HasStatus(MNKBuff.演武))
                    {
                        // 未来如果打不出一组完整的必杀技，返回一个震脚
                        if(Core.Me.GetStatusLeftTime(MNKBuff.义结金兰) <= 8f) return new CheckResult(true, "必杀技即将溢出 直接打出");
                        return new CheckResult(false, "有演武 先打一个演武");
                    }
                        
                    return new CheckResult(true, "团辅内可以打一个震脚");
                }
            }
            else if (PromeSettings.Instance.GetQt(MNKQt.最终爆发))
            {
                return new CheckResult(true, "最终爆发");
            }
        }
        
        return new CheckResult(false, "未冷却");
    }

    public PAction GetAction()
    {
        return new PAction(MNKSkill.震脚, ActionType.OffGcd, ActionTargetType.Self);
    }
}
