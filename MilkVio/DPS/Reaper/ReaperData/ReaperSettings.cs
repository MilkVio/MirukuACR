namespace MilkVio.DPS.Reaper.ReaperData;

public class ReaperSettings
{
    public static ReaperSettings Instance { get; set; } = new();

    // 设置页的checkbox，不再走QT
    public bool 启用起手 = true;
    public bool 倒计时自动播魂 = true;
    public float 远离完人极限释放阈值 = 6f;

    private float? _勾刃倒数预读时间;
    // 未手动修改时沿用实际咏唱时间加0.4秒；自定义后表示倒数剩余的秒数。
    public float 勾刃倒数预读时间
    {
        get => _勾刃倒数预读时间 ?? (ReaperHelper.读取咏唱时间(ReaperSkill.勾刃) ?? 1.3f) + 0.4f;
        set => _勾刃倒数预读时间 = float.IsFinite(value) ? Math.Clamp(value, 0.1f, 10f) : null;
    }
}
