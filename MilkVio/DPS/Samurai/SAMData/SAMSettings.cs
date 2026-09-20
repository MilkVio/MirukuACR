namespace MilkVio.DPS.Samurai.SAMData;

public class SAMSettings
{
    public static SAMSettings Instance { get; set; } = new();

    // 仅保留本次运行的选择。
    public bool 启用起手 = true;
    public bool 显示技能预测;
    public bool 显示下G横幅;
    public bool 锁定预测窗口;
    public float 预测图标大小 = 72;
    public bool 重置预测位置;
}
