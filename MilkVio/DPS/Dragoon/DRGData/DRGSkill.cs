using FFXIVClientStructs.FFXIV.Client.UI;

namespace MilkVio.DPS.Dragoon.DRGData;

public static class DRGSkill
{
    public static uint 精准刺 = 75;    // 1
    
    public static uint 贯通刺 = 78;    // 2 直刺连
    public static uint 前冲刺 = 36954; // 2 直刺连 96级学会
    public static uint 直刺 = 84;     // 3 直刺连
    public static uint 苍穹刺 = 25771; // 3 直刺连 86级学会
    public static uint 龙牙龙爪 = 3554; // 4 直刺连
    
    public static uint 开膛枪 = 87;     // 2 樱花连
    public static uint 螺旋击 = 36955;  // 2 樱花连 96级学会
    public static uint 樱花怒放 = 88;    // 3 樱花连
    public static uint 樱花缭乱 = 25772; // 3 樱花连 86级学会
    public static uint 龙尾大回旋 = 3556;  // 4 樱花连
    
    
    public static uint 云蒸龙变 = 36952; // 任意连击结束
    public static uint 龙眼雷电 = 16479; // 云蒸龙变结束 76级学会
    public static uint 贯穿尖 = 90;
    
    // === AOE ===
    public static uint 死天枪 = 86; // 1
    public static uint 音速刺 = 7397; // 2
    public static uint 山境酷刑 = 16477;  // 3
    public static uint 龙眼苍穹 = 25770; // 山境酷刑结束 86级学会 这技能应该用不到了
    
    // === 伤害OGCD ===
    public static uint 跳跃 = 92;
    public static uint 高跳 = 16478; // 跳跃升级版 74级
    public static uint 幻象冲 = 7399; // 上述技能派生
    
    public static uint 龙炎冲 = 96;
    public static uint 龙炎升 = 36953; // 92级 龙炎冲派生
    
    public static uint 武神枪 = 3555;
    public static uint 死者之岸 = 7400; // 武神枪70级派生
    public static uint 坠星冲 = 16480; // 武神枪80级派生
    public static uint 渡星冲 = 36956; // 坠星冲100级派生

    public static uint 天龙点睛 = 25773; // 90级
    
    // === 增益OGCD ===
    public static uint 龙剑 = 83;
    public static uint 猛枪 = 85;
    public static uint 战斗连祷 = 3557;
    
    // === 杂项 ===
    public static uint 龙翼滑翔 = 36951;
}
