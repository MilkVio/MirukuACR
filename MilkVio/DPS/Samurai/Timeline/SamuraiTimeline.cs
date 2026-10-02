using ECommons.DalamudServices;
using ECommons.ExcelServices;
using MilkVio.DPS.Samurai.Level100;
using MilkVio.DPS.Samurai.SAMData;
using PromeRotation.Data;

namespace MilkVio.DPS.Samurai.Timeline;

internal static class SamuraiTimeline
{
    private static uint _player;
    public static bool MirrorPending { get; private set; }
    public static string Status { get; private set; } = "没有明镜请求";

    public static void RequestMeikyo(bool waitForCharge)
    {
        Update();
        if (Core.Me == null || Core.Me.ClassJob.RowId != (uint)Job.SAM || Core.Me.Level != 100)
        { Svc.Chat.PrintError("[SAM] 请求一次明镜仅支持百级武士"); return; }
        if (MirrorPending) { Samurai100Planning.WriteNote?.Invoke("已有一次明镜请求，不重复排入"); return; }
        if (!waitForCharge && SamuraiHelper.明镜止水层数() < 1)
        {
            Status = "当前无明镜层数 已丢弃";
            Svc.Chat.PrintError(Status);
            Samurai100Planning.WriteNote?.Invoke(Status);
            return;
        }
        MirrorPending = true; _player = Core.Me.EntityId;
        Status = waitForCharge ? "已请求一次明镜，等待充能与合适位置" : "已请求一次明镜，等待合适位置";
        Samurai100Planning.WriteNote?.Invoke(Status);
    }

    public static void Update()
    {
        if (MirrorPending && (Core.Me == null || Core.Me.EntityId != _player ||
            Core.Me.ClassJob.RowId != (uint)Job.SAM || Core.Me.Level != 100)) ClearRequest("离开百级武士");
    }

    public static void ClearRequest(string reason)
    {
        if (MirrorPending) Samurai100Planning.WriteNote?.Invoke($"明镜请求结束：{reason}");
        MirrorPending = false; _player = 0; Status = $"没有明镜请求（{reason}）";
    }

    // 普通重算、死亡和暂时无目标不取消当前战斗的时间轴控制。
    public static void ResetSession(string reason)
    {
        ClearRequest(reason);
        if (PromeSettings.Instance.GetQt(SAMQt.强制垫刀)) Samurai100Planning.WriteNote?.Invoke($"强制垫刀已关闭：{reason}");
        PromeSettings.Instance.SetQt(SAMQt.强制垫刀, false);
        if (PromeSettings.Instance.GetQt(SAMQt.延迟回返)) Samurai100Planning.WriteNote?.Invoke($"延迟回返已关闭：{reason}");
        PromeSettings.Instance.SetQt(SAMQt.延迟回返, false);
    }
}
