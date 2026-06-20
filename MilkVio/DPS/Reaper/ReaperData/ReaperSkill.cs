namespace MilkVio.DPS.Reaper.ReaperData;

public static class ReaperSkill
{
    public const uint 切割 = 24373;        // 1：123 都给 10 蓝，GCD
    public const uint 增盈切割 = 24374;    // 2，GCD
    public const uint 地狱切割 = 24375;    // 3，GCD

    public const uint 旋转钐割 = 24376;    // AOE 1，GCD，AOE 三目标应该才赚
    public const uint 噩梦钐割 = 24377;    // AOE 2，GCD

    public const uint 死亡之影 = 24378;    // 单体死亡烙印，30 秒 10% 增伤，GCD
    public const uint 死亡之涡 = 24379;    // 群体死亡烙印，30 秒 10% 增伤，GCD

    public const uint 灵魂切割 = 24380; // 30 秒一个，给 50 蓝，GCD
    public const uint 灵魂钐割 = 24381; // AOE版本

    public const uint 绞决 = 24382;        // 侧身位，GCD
    public const uint 缢杀 = 24383;        // 背身位，GCD
    public const uint 断首 = 24384;        // 身位技能的 AOE 版本，GCD

    public const uint 大丰收 = 24385;      // 90 级，团辅祭品，GCD
    public const uint 勾刃 = 24386;        // 读条远程止损，GCD
    public const uint 播魂种 = 24387;      // 90 级，5 秒读条，获得播魂种 buff 2594，GCD
    public const uint 收获月 = 24388;      // GCD

    public const uint 隐匿切割 = 24389;    // 消耗 50 蓝，给妖异之镰 buff 2587，OffGCD
    public const uint 绞决爪 = 24390;      // 使用缢杀后，隐匿切割变化而来，GCD
    public const uint 缢杀爪 = 24391;      // 使用绞决后，隐匿切割变化而来，GCD

    public const uint 束缚挥割 = 24392;    // 上面两个收割的 AOE 版本，GCD
    public const uint 暴食 = 24393;        // 消耗 50 蓝，60s CD，获得两层妖异之镰 buff 2587，OffGCD

    public const uint 夜游魂衣 = 24394;    // 获得 buff 2593，OffGCD；100 级再获得祭牲预备 3857
    public const uint 虚无收割 = 24395;    // GCD
    public const uint 交错收割 = 24396;    // GCD
    public const uint 团契 = 24398;        // 90 级，GCD；使用后会直接结束 buff 2593

    public const uint 夜游魂收割 = 24399;  // 90 级，OffGCD
    public const uint 夜游魂钐割 = 24400;  // 90 级，AOE 版本，OffGCD

    public const uint 神秘纹 = 24404;      // 自保，OffGCD
    public const uint 神秘环 = 24405;      // 团辅，OffGCD

    public const uint 祭性 = 36969;        // 100 级，OffGCD
    public const uint 缢杀处刑 = 36970;    // 100 级，背身位，GCD
    public const uint 绞决处刑 = 36971;    // 100 级，侧身位，GCD
    public const uint 断首处刑 = 36972;    // 100 级，上面两个的 AOE 版本，GCD
    public const uint 完人 = 36973;        // 100 级，GCD
}
