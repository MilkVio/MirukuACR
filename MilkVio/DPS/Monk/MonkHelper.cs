using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.JobGauge.Enums;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.DPS.Monk.MNKData;

namespace MilkVio.DPS.Monk;

public static class MonkHelper
{
    public static float GetCurrentMeleeRange()
    {
        if (PromeSettings.Instance.Hacks.IncreaseAttackRange) return 6f;
        return 3f;
    }
    
    public static float GetCurrentAttackRange(float range)
    {
        if (PromeSettings.Instance.Hacks.IncreaseAttackRange) return range + 3f;
        return range;
    }

    public static PAction GetBaseAoeActionNormal()
    {
        // 无论任何情况都应该是魔猿最先？
        // 这里以是否有演武来区分
        var Has演武 = Core.Me.HasStatus(MNKBuff.演武);
        
        if (!Has演武)
        {
            // 通过量谱确定该身形具体该打什么
            var CurrentBeastGroup = GetCurrentBeastGroup();
            switch (CurrentBeastGroup)
            {
                case BeastType.Opo:
                    // 存在魔猿身形
                    return GetOpoAoePAction();
                case BeastType.Rap:
                    // 存在盗龙身形
                    return new PAction(MNKSkill.四面脚, ActionType.Gcd, ActionTargetType.Self);
                case BeastType.Coe:
                    // 存在猛豹身形
                    return new PAction(MNKSkill.地烈劲, ActionType.Gcd, ActionTargetType.Self);
            }
        }
        else if (Has演武)
        {
            if(JobGaugeHelper.MNK.OpoOpoFury == 1) return GetOpoPAction();
            return new PAction(MNKSkill.双龙脚, ActionType.Gcd, ActionTargetType.Target);
        }
        return new PAction(MNKSkill.双龙脚, ActionType.Gcd, ActionTargetType.Target);
    }
    
    public static PAction GetBaseActionNormal()
    {
        // 无论任何情况都应该是魔猿最先？
        // 这里以是否有演武来区分
        var Has演武 = Core.Me.HasStatus(MNKBuff.演武);
        
        if (!Has演武)
        {
            // 通过检测上一个Combo的ID得到下一个身形连击组（123）
            // 然后再通过量谱确定该身形具体该打什么
            var CurrentBeastGroup = GetCurrentBeastGroup();
            switch (CurrentBeastGroup)
            {
                case BeastType.Opo:
                    // 存在魔猿身形
                    if(JobGaugeHelper.MNK.OpoOpoFury == 1) return GetOpoPAction();
                    return new PAction(MNKSkill.双龙脚, ActionType.Gcd, ActionTargetType.Target);
                case BeastType.Rap:
                    // 存在盗龙身形
                    if(JobGaugeHelper.MNK.RaptorFury == 1) return GetRapPAction();
                    return new PAction(MNKSkill.双掌打, ActionType.Gcd, ActionTargetType.Target);
                case BeastType.Coe:
                    // 存在猛豹身形
                    if(JobGaugeHelper.MNK.CoeurlFury > 0) return GetCoePAction();
                    return new PAction(MNKSkill.破碎拳, ActionType.Gcd, ActionTargetType.Target);
            }
        }
        else if (Has演武)
        {
            if(JobGaugeHelper.MNK.OpoOpoFury == 1) return GetOpoPAction();
            return new PAction(MNKSkill.双龙脚, ActionType.Gcd, ActionTargetType.Target);
        }
        return new PAction(MNKSkill.双龙脚, ActionType.Gcd, ActionTargetType.Target);
    }
    
    /// <summary>
    /// 传入震脚获取最优解
    /// </summary>
    /// <param name="needNadi">需要打的阴阳</param>
    /// <returns>PAction</returns>
    public static PAction GetBaseActionPerfect(NadiType needNadi, bool isAOE)
    {
        // 通过needNadi是否为无参数分割
        if (needNadi == NadiType.无参数)
        {
            var nadi = GetCurrentNadi();
            //todo
            /*if (nadi == NadiType.无 && IsInBrotherhood())
            {
                if(isAOE) return GetLunarPAction(true);
                return GetLunarPAction(false);
            }*/
            switch (nadi)
            {
                case NadiType.阴:
                    if(isAOE) return GetSolarPAction(true);
                    return GetSolarPAction(false);
                case NadiType.阳:
                    if(isAOE) return GetLunarPAction(true);
                    return GetLunarPAction(false);
                case NadiType.阴阳:
                    if(isAOE) return GetLunarPAction(true);
                    return GetLunarPAction(false);
                case NadiType.无:
                    if(isAOE) return GetSolarPAction(true);
                    return GetSolarPAction(false);
            }
        }

        switch (needNadi)
        {
            case NadiType.阴:
                if(isAOE) return GetLunarPAction(true);
                return GetLunarPAction(false);
            case NadiType.阳:
                if(isAOE) return GetSolarPAction(true);
                return GetSolarPAction(false);
        }
        

        return null;
    }
    
    /// <summary>
    /// 给身位指示提供的，不要用。
    /// </summary>
    /// <returns></returns>
    public static NadiType GetNextNadi()
    {
        var nadi = GetCurrentNadi();

        switch (nadi)
        {
            case NadiType.阴:
                return NadiType.阳;
            case NadiType.阳:
                return NadiType.阴;
            case NadiType.阴阳:
                return NadiType.阴;
            case NadiType.无:
                return NadiType.阴;
        }
        return NadiType.阴;
    }
    
    #region 私有辅助方法
    
    /// <summary>
    /// 获取当前魔猿AOE 实际需要打出去的等级适配后技能
    /// </summary>
    /// <returns></returns>
    private static PAction GetOpoAoePAction()
    {
        if(Core.Me.Level >= 82) return new PAction(MNKSkill.破坏神脚, ActionType.Gcd, ActionTargetType.Self);
        return new PAction(MNKSkill.破坏神冲, ActionType.Gcd, ActionTargetType.Self);
    }
    
    /// <summary>
    /// 获取当前需要身形豆子的魔猿身形 实际需要打出去的等级适配后技能
    /// </summary>
    /// <returns></returns>
    private static PAction GetOpoPAction()
    {
        if(Core.Me.Level >= 92) return new PAction(MNKSkill.猿舞连击, ActionType.Gcd, ActionTargetType.Target);
        return new PAction(MNKSkill.连击, ActionType.Gcd, ActionTargetType.Target);
    }
    /// <summary>
    /// 获取当前需要身形豆子的盗龙身形 实际需要打出去的等级适配后技能
    /// </summary>
    /// <returns></returns>
    private static PAction GetRapPAction()
    {
        if(Core.Me.Level >= 92) return new PAction(MNKSkill.龙颚正拳, ActionType.Gcd, ActionTargetType.Target);
        return new PAction(MNKSkill.正拳, ActionType.Gcd, ActionTargetType.Target);
    }
    /// <summary>
    /// 获取当前需要身形豆子的魔猿身形 实际需要打出去的等级适配后技能
    /// </summary>
    /// <returns></returns>
    private static PAction GetCoePAction()
    {
        if(Core.Me.Level >= 92) return new PAction(MNKSkill.豹袭崩拳, ActionType.Gcd, ActionTargetType.Target);
        return new PAction(MNKSkill.崩拳, ActionType.Gcd, ActionTargetType.Target);
    }
    
    /// <summary>
    /// 获取阴要打什么 并且转换实际需要打出去的等级适配后技能
    /// </summary>
    private static PAction GetLunarPAction(bool isAOE)
    {
        if (isAOE)
        {
            return GetOpoAoePAction();
        }
        
        if(JobGaugeHelper.MNK.OpoOpoFury == 1) return GetOpoPAction();
        return new PAction(MNKSkill.双龙脚, ActionType.Gcd, ActionTargetType.Target);
    }
    
    /// <summary>
    /// 获取阳要打什么 并且转换实际需要打出去的等级适配后技能
    /// </summary>
    private static PAction GetSolarPAction(bool isAOE)
    {
        var beastType = AutoGetSolarBeastType();
        switch (beastType)
        {
            case BeastType.Opo:
                // 存在魔猿身形
                if (isAOE) return GetOpoAoePAction();
                
                if(JobGaugeHelper.MNK.OpoOpoFury == 1) return GetOpoPAction();
                return new PAction(MNKSkill.双龙脚, ActionType.Gcd, ActionTargetType.Target);
            case BeastType.Rap:
                // 存在盗龙身形
                if (isAOE) return new PAction(MNKSkill.四面脚, ActionType.Gcd, ActionTargetType.Self);
                
                if(JobGaugeHelper.MNK.RaptorFury == 1) return GetRapPAction();
                return new PAction(MNKSkill.双掌打, ActionType.Gcd, ActionTargetType.Target);
            case BeastType.Coe:
                // 存在猛豹身形
                if (isAOE) return new PAction(MNKSkill.地烈劲, ActionType.Gcd, ActionTargetType.Self);
                
                if(JobGaugeHelper.MNK.CoeurlFury > 0) return GetCoePAction();
                return new PAction(MNKSkill.破碎拳, ActionType.Gcd, ActionTargetType.Target);
        }
        return null;
    }
    
    #endregion
    
    /// <summary>
    /// 通过Buff检测当前的身形
    /// </summary>
    /// <returns>当前连击的身形组</returns>
    public static BeastType GetCurrentBeastGroup()
    {
        if (Core.Me.HasStatus(MNKBuff.魔猿身形)) return BeastType.Opo;
        if (Core.Me.HasStatus(MNKBuff.盗龙身形)) return BeastType.Rap;
        if (Core.Me.HasStatus(MNKBuff.猛豹身形)) return BeastType.Coe;
        return BeastType.None;
    }
    
    /// <summary>
    /// 获取当前的阴阳脉轮
    /// </summary>
    /// <returns></returns>
    public static NadiType GetCurrentNadi()
    {
        var nadi = JobGaugeHelper.MNK.Nadi;
        bool hasLunar = nadi.HasFlag(Nadi.Lunar);
        bool hasSolar = nadi.HasFlag(Nadi.Solar);

        if (hasLunar && hasSolar)
            return NadiType.阴阳;
        if (hasLunar)
            return NadiType.阴;
        if (hasSolar)
            return NadiType.阳;
        return NadiType.无;
    }
    
    /// <summary>
    /// 是否能使用阴阳斗气斩 还有那个AOE
    /// </summary>
    public static bool CanUseChakra()
    {
        if(JobGaugeHelper.MNK.Chakra >= 5) return true;
        return false;
    }
    
    /// <summary>
    /// 通过量谱检测阳应该打什么
    /// </summary>
    public static BeastType AutoGetSolarBeastType()
    {
        // 处理量谱列表
        // 过滤出所有已经打了的豆子
        var used = JobGaugeHelper.MNK.BeastChakra.Where(c => c != BeastChakra.None).ToList();
        // 定义一个用来过滤的临时列表
        var allTypes = new List<BeastChakra>
        {
            BeastChakra.OpoOpo, // 魔猿
            BeastChakra.Raptor, // 盗龙
            BeastChakra.Coeurl  // 猛豹
        };
        // 在allTypes中用used过滤出剩余未打出的身形
        var remaining = allTypes.Except(used).ToList();
        // 如果三豆都打完了，直接返回 None
        if (remaining.Count == 0)
            return BeastType.None;
        
        // 位置处理
        var pos = TargetHelper.GetTargetPositional();
        
        var coeCurrentPos = JobGaugeHelper.MNK.CoeurlFury>0 ? Positional.Flank : Positional.Rear;
        
        // 开始逻辑 按是否拥有猛豹区分
        if (remaining.Contains(BeastChakra.Coeurl))
        {
            if (coeCurrentPos == pos) return BeastType.Coe;
            
            var safeOptions = remaining.Where(t => t != BeastChakra.Coeurl).ToList();
            if (safeOptions.Count > 0)
            {
                // 这里按顺序打：先魔猿，没魔猿就盗龙
                var pick = safeOptions.Contains(BeastChakra.OpoOpo) ? BeastChakra.OpoOpo : BeastChakra.Raptor;
                return pick == BeastChakra.OpoOpo ? BeastType.Opo : BeastType.Rap;
            }
            
            // 如果不需要身位都用完了，强行打猛豹
            return BeastType.Coe;
        }
        
        var pick2 = remaining.First();
        return pick2 switch
        {
            BeastChakra.OpoOpo => BeastType.Opo,
            BeastChakra.Raptor => BeastType.Rap,
            _ => BeastType.None
        };
    }

    public static bool IsInBrotherhood()
    {
        if (Core.Me.HasStatus(MNKBuff.义结金兰)) return true;
        return false;
    }
    
    public static bool IsInRof()
    {
        if (Core.Me.HasStatus(MNKBuff.红莲极意)) return true;
        return false;
    }
    
    public static bool IsIn120()
    {
        if (Core.Me.HasStatus(MNKBuff.义结金兰)) return true;
        return false;
    }
    
    public static bool IsIn60()
    {
        if (IsInRof() && MNKSkill.义结金兰.GetActionCooldown() > 10 && !Core.Me.HasStatus(MNKBuff.义结金兰)) return true;
        return false;
    }
    
    public static bool IsInPrue120()
    {
        if (Core.Me.HasStatus(MNKBuff.义结金兰) && Core.Me.HasStatus(MNKBuff.红莲极意)) return true;
        return false;
    }
    
    public static float GetRealPbCharge()
    {
        const float MaxCharges = 2f;  // 震脚最大层数
        const float PerCharge  = 40f; // 单层冷却时间

        // 返回距离充满还剩多少秒（0~80）
        float cdToFull = MNKSkill.震脚.GetActionCooldown();

        // Clamp：防止出界（如果小于0或大于80）
        if (cdToFull < 0f) cdToFull = 0f;
        if (cdToFull > MaxCharges * PerCharge) cdToFull = MaxCharges * PerCharge;

        // 计算浮点层数
        float real = MaxCharges - (cdToFull / PerCharge);
        if (real < 0f) real = 0f;
        if (real > MaxCharges) real = MaxCharges;

        return real;
    }

    public static void RestQt()
    {
        var QtList = new Dictionary<string, bool>
        {
            { MNKQt.启用起手, true },
            { MNKQt.不打60, false },
            { MNKQt.不打120, false },
            { MNKQt.AOE, false },
            { MNKQt.强制对齐爆发, false },
            { MNKQt.延后绝空拳, false },
            { MNKQt.疾风对齐120, false },
            { MNKQt.倾泻资源, false },
            { MNKQt.最终爆发, false },
            { MNKQt.攒资源, false },
            { MNKQt.自动演武, true },
            { MNKQt.搓豆子, true },
            { MNKQt.必杀技, true },
            { MNKQt.震脚打阴, false },
            { MNKQt.震脚打阳, false },
            { MNKQt.无目标搓必杀技, false },
            { MNKQt.TP身位, false },
        };
        
        foreach (var (name, def) in QtList)
            PromeSettings.Instance.SetQt(name, def);
    }
}
