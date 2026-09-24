namespace MilkVio.DPS.Machinist.MCHData;

public class MCHSettings
{
    public static MCHSettings Instance { get; set; } = new();

    // 仅保留本次运行的选择。
    public bool 启用起手 = true;
}
