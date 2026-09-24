using System.Reflection;
using PromeRotation.Data;
using PromeRotation.Managers;
using PromeRotation.Updaters;

namespace MilkVio.DPS.Machinist.Planning;

// 只使用现有宿主接口。preview.7没有导出已有的插入上限设置，保留公开属性兼容入口。
internal static class MachinistHost
{
    private static readonly PropertyInfo? Limit = typeof(HackSettings).GetProperty("MaxOgcdsPerGcd");
    public static int MaxWeaves => Limit?.GetValue(PromeSettings.Instance.Hacks) is int value ? Math.Clamp(value, 1, 2) : 2;
    public static bool Busy => ActionUpdater.HasActiveCommand() || ActionQueueManager.HasActionsInQueue();
}
