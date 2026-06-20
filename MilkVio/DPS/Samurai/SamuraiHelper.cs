using PromeRotation.Core;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai;

public static class SamuraiHelper
{
    /// <summary>
    /// 获取1连击调整后的Id
    /// </summary>
    /// <param name="isAoe">是否为AOE</param>
    /// <returns>Id</returns>
    public static uint Get1ComboActionId(bool isAoe)
    {
        var level = Core.Me.Level;
        
        if (isAoe)
        {
            if (level < 92)
            {
                return SAMSkill.风雅;
            }
            return SAMSkill.风光;
        }
        
        if (level < 92)
        {
            return SAMSkill.刃风;
        }
        return SAMSkill.晓风;
    }
    
    public static bool 回返斩浪可用()
    {
        return SAMSkill.奥义斩浪.GetAdjustedActionId() == SAMSkill.回返斩浪;
    }
    
    public static bool IsComboEnd()
    {
        var lastActionId = ActionHelper.GetLastComboID();
        if (lastActionId == 0) return true;
        
        if (lastActionId == SAMSkill.月光 || lastActionId == SAMSkill.花车 || lastActionId == SAMSkill.雪风 ||
            lastActionId == SAMSkill.满月 || lastActionId == SAMSkill.樱花)
        {
            return true;
        }
        
        return false;
    }
    
    public static 居合类型 GetCurrent居合类型()
    {
        var count = JobGaugeHelper.SAM.GetSenCount();
        var has天道 = Core.Me.HasStatus(SAMBuff.天道);
        
        if (count == 2 && has天道)
        {
            return 居合类型.天道雪月花;
        }
        if (count == 3 && has天道)
        {
            return 居合类型.天道五剑;
        }
        
        switch (count)
        {
            case 0:
                return 居合类型.无;
            case 1:
                return 居合类型.彼岸花;
            case 2:
                return 居合类型.天下五剑;
            case 3:
                return 居合类型.雪月花;
            default:
                return 居合类型.无;
        }
    }
    
    /*
     * 目前可以想到的盘子难点
     * 明镜的开启时机，或者说整个盘子都是要围绕明镜来处理的：
     * 倾泻资源状态：只要缺1闪就开 不管当前combo
     * 常规状态：当前combo必须完结同时当前缺的闪不是雪
     */
    
    /*
     * 双彼岸花考虑在updater中设置一个参数 先不考虑这个
     * 居合术：
     * 彼岸花：当前目标剩余彼岸花<3 或者 没有彼岸花 同时开了QT
     * 雪月花：不满足彼岸花条件 如果开启了AOE 则检测周围是否？这个应该不需要
     * 天下五剑：开启AOEQT 周围人数>=3
     */
    
    /*
     * 常规连击：
     * 这里应该可以以buff剩余时间区分
     * α 双buff都>15s 按照身位/闪来自动调整循环
     * β 双buff任意<15s或者没有 按照续最短buff 月 > 樱
     * α：
     * 如果在背身 没有月就打月 有月看有没有雪 没有雪先打雪 有雪最后备选打樱
     * 如果在侧身 没有花就打花 有花看有没有雪 没有雪先打雪 有雪最后备选打月
     * 默认情况：按照优先级雪>月>花打
     * β：
     * if从上到下 月 > 花
     * 月buff<15s && 没有月 打月
     * 花buff<15s && 没有花 打花
     * 默认打雪
     */

    public static Combo类型 GetBestComboType()
    {
        var me = Core.Me;
        var moonLeftTime = me.GetStatusLeftTime(SAMBuff.风月);
        var hanaLeftTime = me.GetStatusLeftTime(SAMBuff.风花);
        var has花 = JobGaugeHelper.SAM.HasHana;
        var has月 = JobGaugeHelper.SAM.HasMoon;
        var has雪 = JobGaugeHelper.SAM.HasYuki;
        var positional = TargetHelper.GetTargetPositional();
        
        // αMode
        if (moonLeftTime >= 15 && hanaLeftTime >= 15)
        {
            // 后方
            if (positional == Positional.Rear)
            {
                if (!has雪)
                {
                    return Combo类型.雪;
                }
                
                if (!has月)
                {
                    return Combo类型.月;
                }
                
                return Combo类型.花;
            }
            // 侧方
            if (positional == Positional.Flank)
            {
                if (!has雪)
                {
                    return Combo类型.雪;
                }
                if (!has花)
                {
                    return Combo类型.花;
                }
                
                return Combo类型.月;
            }
            if (positional == Positional.Front || positional == Positional.None)
            {
                if (!has雪)
                {
                    return Combo类型.雪;
                }

                if (!has月)
                {
                    return Combo类型.月;
                }
                
                if (!has花)
                {
                    return Combo类型.花;
                }
            }
        }
        
        // βMode
        if (moonLeftTime < 15 || hanaLeftTime < 15)
        {
            if (moonLeftTime < 15 && !has月)
            {
                return Combo类型.月;
            }
            if (hanaLeftTime < 15 && !has花)
            {
                return Combo类型.花;
            }
        }

        return Combo类型.无;
    }

    public static PAction? GetCurrentMsyPAction()
    {
        var lastComboId = ActionHelper.GetLastComboID();
        var bestComboType = GetBestComboType();
        // 选择分支
        
        if (lastComboId == SAMSkill.阵风 && JobGaugeHelper.SAM.GetSenCount() != 3)
        {
            return new PAction(SAMSkill.月光, ActionType.Gcd, ActionTargetType.Target);
        }
        
        if (lastComboId == SAMSkill.士风 && JobGaugeHelper.SAM.GetSenCount() != 3)
        {
            return new PAction(SAMSkill.花车, ActionType.Gcd, ActionTargetType.Target);
        }
        
        if (lastComboId == Get1ComboActionId(false))
        {
            if (bestComboType != Combo类型.无)
            {
                switch (bestComboType)
                {
                    case Combo类型.雪:
                        return new PAction(SAMSkill.雪风, ActionType.Gcd, ActionTargetType.Target);
                    case Combo类型.月:
                        return new PAction(SAMSkill.阵风, ActionType.Gcd, ActionTargetType.Target);
                    case Combo类型.花:
                        return new PAction(SAMSkill.士风, ActionType.Gcd, ActionTargetType.Target);
                }
            }
            return new PAction(SAMSkill.雪风, ActionType.Gcd, ActionTargetType.Target);
        }
        
        return null;
    }

    public static 居合类型 GetBestJuhe()
    {
        var isTargetHasBianhua = Core.Target.HasStatus(SAMBuff.彼岸花);
        var bianhuaLeftTime = Core.Target.GetStatusLeftTime(SAMBuff.彼岸花);
        
        if (JobGaugeHelper.SAM.GetSenCount() == 1)
        {
            if ((!isTargetHasBianhua || bianhuaLeftTime < 3) && PromeSettings.Instance.GetQt(SAMQt.彼岸花) && !PromeSettings.Instance.GetQt(SAMQt.倾泻资源))
            {
                return 居合类型.彼岸花;
            }
        }
        
        if (JobGaugeHelper.SAM.GetSenCount() == 2)
        {
            if (TargetHelper.EnemyInRange(8) >= 3 && PromeSettings.Instance.GetQt(SAMQt.AOE))
            {
                return 居合类型.天下五剑;
            }
        }
        
        if (JobGaugeHelper.SAM.GetSenCount() == 3)
        {
            return 居合类型.雪月花;
        }
        
        return 居合类型.无;
    }

    public static Combo类型 GetBestMingJingType()
    {
        var me = Core.Me;
        var moonLeftTime = me.GetStatusLeftTime(SAMBuff.风月);
        var hanaLeftTime = me.GetStatusLeftTime(SAMBuff.风花);
        var has花 = JobGaugeHelper.SAM.HasHana;
        var has月 = JobGaugeHelper.SAM.HasMoon;
        var has雪 = JobGaugeHelper.SAM.HasYuki;
        
        // 确认组
        if (moonLeftTime < 15 && !has月)
        {
            return Combo类型.月;
        }
        if (hanaLeftTime < 15 && !has花)
        {
            return Combo类型.花;
        }
        // 正常组
        if (!has月)
        {
            return Combo类型.月;
        }
        if (!has花)
        {
            return Combo类型.花;
        }

        if (!has雪)
        {
            return Combo类型.雪;
        }

        return Combo类型.无;
    }
    
    // 燕回返工具组
    public static bool Has燕回返()
    {
        var me = Core.Me;
        if (me.HasStatus(SAMBuff.燕回返预备1) || me.HasStatus(SAMBuff.燕回返预备2) || me.HasStatus(SAMBuff.燕回返预备3) ||
            me.HasStatus(SAMBuff.燕回返预备4))
        {
            return true;
        }

        return false;
    }

    public static float 燕回返LeftTime()
    {
        var me = Core.Me;
        if (me.GetStatusLeftTime(SAMBuff.燕回返预备1) > 0)
        {
            return me.GetStatusLeftTime(SAMBuff.燕回返预备1);
        }
        if (me.GetStatusLeftTime(SAMBuff.燕回返预备2) > 0)
        {
            return me.GetStatusLeftTime(SAMBuff.燕回返预备2);
        }
        if (me.GetStatusLeftTime(SAMBuff.燕回返预备3) > 0)
        {
            return me.GetStatusLeftTime(SAMBuff.燕回返预备3);
        }
        if (me.GetStatusLeftTime(SAMBuff.燕回返预备4) > 0)
        {
            return me.GetStatusLeftTime(SAMBuff.燕回返预备4);
        }
        return 0;
    }

    public static bool IsInSelf120()
    {
        var cd = SAMSkill.意气冲天.GetActionCooldown();
        if (cd == 0) return false;
        if (cd >= 100)
        {
            return true;
        }
        return false;
    }
    
    public static float 明镜止水层数()
    {
        var player = Core.Me;
        float MaxCharges = 2f;  // 最大层数
        float PerCharge  = 55f; // 单层冷却时间
        // 返回距离充满还剩多少秒
        float cdToFull = SAMSkill.明镜止水.GetActionCooldown();
        
        // 等级适配
        if (player.Level < 76)
        {
            MaxCharges = 1f;
            //cdToFull -= 1 * PerCharge;
        }
        
        // 防止出界
        if (cdToFull < 0f) cdToFull = 0f;
        if (cdToFull > MaxCharges * PerCharge) cdToFull = MaxCharges * PerCharge;

        // 计算
        float real = MaxCharges - (cdToFull / PerCharge);
        if (real < 0f) real = 0f;
        if (real > MaxCharges) real = MaxCharges;

        return real;
    }

    public static Positional GetNeedPositional()
    {
        var needPostional = Positional.None;
        var hasMingjing = Core.Me.HasStatus(SAMBuff.明镜止水);
        
        if (hasMingjing)
        {
            var type = SamuraiHelper.GetBestMingJingType();
            switch (type)
            {
                case Combo类型.月:
                    needPostional = Positional.Rear;
                    break;
                case Combo类型.花:
                    needPostional = Positional.Flank;
                    break;
                case Combo类型.雪:
                    needPostional = Positional.None;
                    break;
            }
        }
        else // !hasMingjing
        {
            var msyAction = SamuraiHelper.GetCurrentMsyPAction();

            if (msyAction == null)
            {
                needPostional = Positional.None;
            }
            else
            {
                var actionId = msyAction.ActionId;
                switch (actionId)
                {
                    case SAMSkill.月光:
                        needPostional = Positional.Rear;
                        break;
                    case SAMSkill.花车:
                        needPostional = Positional.Flank;
                        break;
                    default:
                        needPostional = Positional.None;
                        break;
                }
            }
        }

        return needPostional;
    }
}
