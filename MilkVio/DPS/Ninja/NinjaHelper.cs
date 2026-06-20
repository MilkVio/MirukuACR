using System.Collections.Generic;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.DPS.Ninja.NinjaData;

namespace MilkVio.DPS.Ninja;

public static class NinjaHelper
{
    public static PAction GetBaseAction()
    {
        var lastActionId = ActionHelper.GetLastComboID();
        if (lastActionId == NinjaSkill.双刃旋) return new PAction(NinjaSkill.绝风, ActionType.Gcd, ActionTargetType.Target);
        return new PAction(NinjaSkill.双刃旋, ActionType.Gcd, ActionTargetType.Target);
    }

    public static class NinjaNinjyutsu
    {
        public static List<PAction> 雷遁 = new List<PAction>
        {
            new(NinjaSkill.天之印, ActionType.Gcd, ActionTargetType.Self),
            new(NinjaSkill.地之印派生, ActionType.Gcd, ActionTargetType.Self)
        };
        public static List<PAction> 火遁 = new List<PAction>
        {
            new(NinjaSkill.地之印, ActionType.Gcd, ActionTargetType.Self),
            new(NinjaSkill.天之印派生, ActionType.Gcd, ActionTargetType.Self)
        };
        public static List<PAction> 水遁 = new List<PAction>
        {
            new(NinjaSkill.天之印, ActionType.Gcd, ActionTargetType.Self),
            new(NinjaSkill.地之印派生, ActionType.Gcd, ActionTargetType.Self),
            new(NinjaSkill.人之印派生, ActionType.Gcd, ActionTargetType.Self)
        };
        public static List<PAction> 冰晶 = new List<PAction>
        {
            new(NinjaSkill.地之印, ActionType.Gcd, ActionTargetType.Self),
            new(NinjaSkill.人之印派生, ActionType.Gcd, ActionTargetType.Self),
        };
        public static List<PAction> 土遁 = new List<PAction>
        {
            new(NinjaSkill.天之印, ActionType.Gcd, ActionTargetType.Self),
            new(NinjaSkill.人之印派生, ActionType.Gcd, ActionTargetType.Self),
            new(NinjaSkill.地之印派生, ActionType.Gcd, ActionTargetType.Self),
        };
        public static List<PAction> 天地人单体 = new List<PAction>
        {
            new(NinjaSkill.天地人_风魔手里剑, ActionType.Gcd, ActionTargetType.Target),
            new(NinjaSkill.天地人_雷遁之术, ActionType.Gcd, ActionTargetType.Target),
            new(NinjaSkill.天地人_水遁之术, ActionType.Gcd, ActionTargetType.Target),
        };
    }
    
    public static bool Is结印()
    {
        return Core.Me.HasStatus(NinjaBuff.结印);
    }
    
    public static bool Is月影雷兽()
    {
        return Core.Me.HasStatus(NinjaBuff.月影雷兽预备);
    }
    
    public static bool Is120()
    {
        var cd = NinjaSkill.介毒之术.GetActionCooldown();
        if(cd == 0) return false;
        if (cd > 99) return true;
        return false;
    }
    
    public static bool Is60()
    {
        var me = Core.Me;
        float cd;
        if (me.Level < 92)
        {
            cd = NinjaSkill.攻其不备.GetActionCooldown();
        }
        else
        {
            cd = NinjaSkill.百雷铳.GetActionCooldown();
        }
        
        if(cd == 0) return false;
        if (cd > 39) return true;
        return false;
    }

    public static uint GetNinja60ActionId()
    {
        var me = Core.Me;
        var id = NinjaSkill.百雷铳;
        if (me.Level < 92)
        {
            id = NinjaSkill.攻其不备;
        }
        return id;
    }
    
    public static float GetCurrentNinjaNinjyutsuCharge()
    {
        float MaxCharges = 2f;  // 最大层数
        float PerCharge  = 20f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = NinjaSkill.人之印.GetActionCooldown();
        
        // 防止负数计算
        if (cdToFull < 0f) cdToFull = 0f;
        if (cdToFull > MaxCharges * PerCharge) cdToFull = MaxCharges * PerCharge;

        // 计算
        float real = MaxCharges - (cdToFull / PerCharge);
        if (real < 0f) real = 0f;
        if (real > MaxCharges) real = MaxCharges;
        return real;
    }

    // 这里似乎分三种情况
    // 模式1 60 120之前 留两个进去就行
    // 即进爆发前（10s && !=0）攒资源 也就是 风缠<2的时候，强制打 
    // 模式2 根据当前在目标的方位和风缠的数量 自动选择打侧身还是背身爆发前（>10s）
    // 1.在boss正面 没满优先打侧 满了就打背
    // 2.在boss侧面 优先打侧 满了就打背
    // 3.在boss背面 优先打背 没了就打侧
    // 模式3 倾泻资源 只打高威力那个 除非没有风缠
    // 模式12都是根据技能冷却时间来判断的，所以可以以倾泻资源区分
    // 规定必须返回一个侧 或者 背
    public static Positional GetNinjaPositionAction()
    {
        var will60 = GetNinja60ActionId().GetActionCooldown() < 10 && GetNinja60ActionId().GetActionCooldown() > 0;
        var will120 = NinjaSkill.介毒之术.GetActionCooldown() < 10 && NinjaSkill.介毒之术.GetActionCooldown() > 0;
        var positional = TargetHelper.GetTargetPositional();
        var kazematoi = JobGaugeHelper.NIN.Kazematoi;
        
        if (PromeSettings.Instance.GetQt(NinjaQt.倾泻资源))
        {
            // 模式3
            if (kazematoi > 0)
            {
                return Positional.Rear;
            }
            return Positional.Flank;
        }
        
        // 模式1
        if (will60 || will120)
        {
            if (kazematoi < 2)
            {
                return Positional.Flank; // 打侧补充
            }
            return Positional.Rear;
        }
        
        // 模式2
        // 正面侧面逻辑相同
        if (positional == Positional.Front || positional == Positional.Flank)
        {
            if (kazematoi < 4)
            {
                return Positional.Flank; // 正面优先打侧
            }
            return Positional.Rear;
        }
        // 背面
        if (positional == Positional.Rear)
        {
            if (kazematoi > 0)
            {
                return Positional.Rear;
            }
            return Positional.Flank;
        }
        
        return Positional.Flank;
    }
}
