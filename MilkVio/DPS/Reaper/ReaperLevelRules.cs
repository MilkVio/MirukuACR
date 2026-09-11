using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper;

// 7.55。等级与威力是数据；这里不选择循环。100级既有规划不调用这张表。
internal static class ReaperLevelRules
{
    internal static bool UsesLevel90(int level) => level is >= 90 and < 100;
    internal static int SliceCapacity(int level) => level >= 78 ? 2 : 1;
    internal static bool HasSacrificium(int level) => level >= 92;
    internal static bool HasExecutioner(int level) => level >= 96;

    internal static int LearnedAt(uint id) => id switch
    {
        ReaperSkill.切割 => 1, ReaperSkill.增盈切割 => 5, ReaperSkill.死亡之影 => 10,
        ReaperSkill.勾刃 => 15, ReaperSkill.地狱入境 or ReaperSkill.地狱出境 => 20, ReaperSkill.旋转钐割 => 25,
        ReaperSkill.地狱切割 => 30, ReaperSkill.死亡之涡 => 35, ReaperSkill.神秘纹 => 40,
        ReaperSkill.噩梦钐割 => 45, ReaperSkill.隐匿挥割 => 50, ReaperSkill.束缚挥割 => 55,
        ReaperSkill.灵魂切割 => 60, ReaperSkill.灵魂钐割 => 65,
        ReaperSkill.绞决 or ReaperSkill.缢杀 or ReaperSkill.断首 or ReaperSkill.绞决爪 or ReaperSkill.缢杀爪 => 70,
        ReaperSkill.神秘环 => 72, ReaperSkill.回退 => 74, ReaperSkill.暴食 => 76,
        ReaperSkill.夜游魂衣 or ReaperSkill.虚无收割 or ReaperSkill.交错收割 or ReaperSkill.阴冷收割 => 80,
        ReaperSkill.播魂种 or ReaperSkill.收获月 => 82,
        ReaperSkill.夜游魂切割 or ReaperSkill.夜游魂钐割 => 86,
        ReaperSkill.大丰收 => 88, ReaperSkill.团契 => 90, ReaperSkill.祭性 => 92,
        ReaperSkill.绞决处刑 or ReaperSkill.缢杀处刑 or ReaperSkill.断首处刑 => 96,
        ReaperSkill.完人 => 100,
        _ => 0 // 通用能力技的等级仍由宿主检查。
    };

    internal static bool Learned(int level, uint id) => level >= LearnedAt(id);

    internal static int Potency(int level, uint id, bool enhanced = false, bool combo = true,
        bool positional = true, int sacrifices = 1)
    {
        if (!Learned(level, id)) return 0;
        var mastery = level >= 94;
        return id switch
        {
            ReaperSkill.切割 => mastery ? 420 : level >= 84 ? 320 : 300,
            ReaperSkill.增盈切割 => (mastery ? 260 : level >= 84 ? 160 : 140) + (combo ? 240 : 0),
            ReaperSkill.地狱切割 => (mastery ? 280 : level >= 84 ? 180 : 140) + (combo ? 320 : 0),
            ReaperSkill.死亡之影 or ReaperSkill.勾刃 => 300,
            ReaperSkill.灵魂切割 => mastery ? 520 : 460,
            ReaperSkill.隐匿挥割 => 340,
            ReaperSkill.绞决爪 or ReaperSkill.缢杀爪 => mastery ? 440 : 400,
            ReaperSkill.绞决 or ReaperSkill.缢杀 => (mastery ? 500 : 400) + (enhanced ? 60 : 0) + (positional ? 60 : 0),
            ReaperSkill.绞决处刑 or ReaperSkill.缢杀处刑 => 700 + (enhanced ? 60 : 0) + (positional ? 60 : 0),
            ReaperSkill.暴食 => 560,
            ReaperSkill.虚无收割 or ReaperSkill.交错收割 => (mastery ? 580 : 460) + (enhanced ? 60 : 0),
            ReaperSkill.夜游魂切割 => mastery ? 280 : 200,
            ReaperSkill.收获月 => mastery ? 800 : 600,
            ReaperSkill.大丰收 => 720 + 40 * Math.Clamp(sacrifices - 1, 0, 7),
            ReaperSkill.团契 => 1100, ReaperSkill.祭性 => 700, ReaperSkill.完人 => 1300,
            ReaperSkill.旋转钐割 => 140, ReaperSkill.噩梦钐割 => combo ? 180 : 120,
            ReaperSkill.死亡之涡 => 100, ReaperSkill.灵魂钐割 => 180,
            ReaperSkill.束缚挥割 => 140, ReaperSkill.断首 => 200, ReaperSkill.阴冷收割 => 220,
            ReaperSkill.夜游魂钐割 => 100, ReaperSkill.断首处刑 => 260,
            _ => 0
        };
    }

    internal static ReaperState Normalize(ReaperState s) => s with
    {
        Perfectio = 0, Occulta = 0, PerfectioGcd = 0,
        Oblatio = HasSacrificium(s.Level) ? s.Oblatio : 0,
        Executioner = HasExecutioner(s.Level) && s.Executioner,
        DumpQt = s.DumpQt && !s.WindowActive
    };
}
