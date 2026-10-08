using ECommons.DalamudServices;
using ECommons.ExcelServices;
using MilkVio.DPS.Samurai.Level100;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Samurai.Timeline;

internal static class SamuraiHiganbanaBlacklist
{
    private const string RuntimeKey = "MilkVio.SAM.HiganbanaBlacklist";
    private static readonly HashSet<uint> Direct = [];
    private static readonly Dictionary<SharedBlackboard, HashSet<uint>> ByRuntime = [];
    private static uint _player;
    public static System.Action? Changed;

    public static bool BlocksCurrentTarget => Contains(Core.Target?.BaseId ?? 0);

    public static bool Contains(uint baseId)
    {
        Update();
        return baseId != 0 && (Direct.Contains(baseId) || ByRuntime.Values.Any(ids => ids.Contains(baseId)));
    }

    public static void Add(uint baseId, SharedBlackboard? runtime)
    {
        Update();
        if (baseId == 0) { Svc.Chat.PrintError("[SAM] 彼岸花黑名单BaseId无效"); return; }
        if (Core.Me == null || Core.Me.ClassJob.RowId != (uint)Job.SAM)
        { Svc.Chat.PrintError("[SAM] 彼岸花黑名单仅限武士使用"); return; }
        _player = Core.Me.EntityId;
        var ids = Direct;
        if (runtime != null)
        {
            if (!ByRuntime.TryGetValue(runtime, out ids))
            {
                ids = [];
                ByRuntime.Add(runtime, ids);
            }
            runtime.Bools[RuntimeKey] = true;
        }
        if (!ids.Add(baseId)) return;
        Invalidate($"彼岸花黑名单添加：BaseId={baseId}");
    }

    public static void Update()
    {
        if (Direct.Count == 0 && ByRuntime.Count == 0) return;
        if (Core.Me == null || Core.Me.EntityId != _player || Core.Me.ClassJob.RowId != (uint)Job.SAM)
        { Clear("角色或职业变化"); return; }

        // 整条轴结束才清理，单个节点结束仍共用这份数据。
        List<SharedBlackboard>? expired = null;
        foreach (var pair in ByRuntime)
            if (!pair.Key.Bools.TryGetValue(RuntimeKey, out var active) || !active)
                (expired ??= []).Add(pair.Key);
        if (expired == null) return;
        foreach (var runtime in expired) ByRuntime.Remove(runtime);
        Invalidate("彼岸花黑名单移除已结束的时间轴来源");
    }

    public static void Clear(string reason)
    {
        var hadEntries = Direct.Count > 0 || ByRuntime.Count > 0;
        foreach (var runtime in ByRuntime.Keys) runtime.Bools.Remove(RuntimeKey);
        Direct.Clear(); ByRuntime.Clear(); _player = 0;
        if (hadEntries) Invalidate($"彼岸花黑名单清空：{reason}");
    }

    private static void Invalidate(string reason)
    {
        Samurai100Planning.Invalidate();
        Changed?.Invoke();
        Samurai100Planning.WriteNote?.Invoke(reason);
    }
}
