namespace MilkVio.DPS.Reaper.ReaperData;

public static class ReaperSkill
{
    public const uint 切割 = 24373;        // 1：123 都给 10 灵魂，GCD
    public const uint 增盈切割 = 24374;    // 2，GCD
    public const uint 地狱切割 = 24375;    // 3，GCD

    public const uint 旋转钐割 = 24376;    // AOE 1，GCD，AOE 三目标应该才赚
    public const uint 噩梦钐割 = 24377;    // AOE 2，GCD

    public const uint 死亡之影 = 24378;    // 单体死亡烙印，30 秒 10% 增伤，GCD
    public const uint 死亡之涡 = 24379;    // 群体死亡烙印，30 秒 10% 增伤，GCD

    public const uint 灵魂切割 = 24380; // 60级，30秒充能，给50灵魂；78级获得第二层
    public const uint 灵魂钐割 = 24381; // AOE版本

    public const uint 绞决 = 24382;        // 侧身位，GCD
    public const uint 缢杀 = 24383;        // 背身位，GCD
    public const uint 断首 = 24384;        // 身位技能的 AOE 版本，GCD

    public const uint 大丰收 = 24385;      // 88级，消耗团辅祭品，给免费附体；100级才给补完
    public const uint 勾刃 = 24386;        // 读条远程止损，GCD
    public const uint 播魂种 = 24387;      // 82级，战斗中5秒读条，获得播魂种buff 2594，GCD
    public const uint 收获月 = 24388;      // GCD

    public const uint 隐匿挥割 = 24389;    // 消耗50灵魂，70级起给妖异之镰buff 2587，OffGCD
    public const uint 绞决爪 = 24390;      // 70级，隐匿挥割的强化版，OffGCD
    public const uint 缢杀爪 = 24391;      // 70级，隐匿挥割的强化版，OffGCD

    public const uint 束缚挥割 = 24392;    // 上面两个收割的AOE版本，OffGCD
    public const uint 暴食 = 24393;        // 76级，消耗50灵魂，60s CD；96级把两层妖异之镰改为处刑人

    public const uint 夜游魂衣 = 24394;    // 80级职业任务，获得buff 2593，OffGCD；92级起再获得祭牲预备3857
    public const uint 虚无收割 = 24395;    // GCD
    public const uint 交错收割 = 24396;    // GCD
    public const uint 阴冷收割 = 24397;    // 80级，附体AOE，GCD
    public const uint 团契 = 24398;        // 90 级，GCD；使用后会直接结束 buff 2593

    public const uint 夜游魂切割 = 24399;  // 86级，OffGCD
    public const uint 夜游魂钐割 = 24400;  // 86级，AOE版本，OffGCD
    public const uint 地狱入境 = 24401;
    public const uint 地狱出境 = 24402;    // 20级
    public const uint 回退 = 24403;        // 74级

    public const uint 神秘纹 = 24404;      // 自保，OffGCD
    public const uint 神秘环 = 24405;      // 团辅，OffGCD

    public const uint 祭性 = 36969;        // 92级，OffGCD
    public const uint 缢杀处刑 = 36970;    // 96级，GCD
    public const uint 绞决处刑 = 36971;    // 96级，GCD
    public const uint 断首处刑 = 36972;    // 96级，上面两个的AOE版本，GCD
    public const uint 完人 = 36973;        // 100 级，GCD
    
}
