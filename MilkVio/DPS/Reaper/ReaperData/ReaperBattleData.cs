namespace MilkVio.DPS.Reaper.ReaperData;

public class ReaperBattleData
{
    public static ReaperBattleData Instance { get; set; } = new();
    public ReaperBurstPlanner Planner { get; } = new();
    public ReaperOutputWindow Window { get; } = new();
    internal ReaperDmuOpener DmuOpener { get; } = new();
    internal ReaperAutoSoulsow AutoSoulsow { get; } = new();
    public bool FastCircle { get; private set; }
    internal ReaperDebugLog DebugLog { get; private set; } = CreateDebugLog();
    internal string OpenerName { get; set; } = "宿主尚未调用起手选择";

    public ReaperBattleData()
    {
        Window.Changed = (now, text) => DebugLog.Note(now, text);
    }

    private static ReaperDebugLog CreateDebugLog() => new(() => System.IO.Path.Combine(
        ECommons.DalamudServices.Svc.PluginInterface.ConfigDirectory.FullName, "ACR", "MilkVio", "DebugLog"));

    internal void RenewDebugLog()
    {
        DebugLog.Shutdown(Environment.TickCount64);
        DebugLog = CreateDebugLog();
    }

    public void SetFastCircle(bool enabled)
    {
        if (FastCircle == enabled) return;
        FastCircle = enabled;
        DebugLog.Note(Environment.TickCount64, $"时间轴：快速神秘环={(enabled ? "开启" : "关闭")}");
    }

    public void Reset(string reason = "战斗或地图已重置")
    {
        SetFastCircle(false);
        AutoSoulsow.Cancel();
        Planner.Reset(reason);
        Window.Reset(reason);
        DmuOpener.Reset(reason);
    }
}
