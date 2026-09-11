using MilkVio.DPS.Reaper.ReaperData;

namespace MilkVio.DPS.Reaper;

// 派发层只读当前等级的决策。各等级规划器不共享进度、缓存或预测实例。
internal interface IReaperPlanner
{
    ReaperState Current { get; }
    ReaperBurstPhase Phase { get; }
    string Reason { get; }
    string ModeName { get; }
    uint GcdAction { get; }
    uint OffGcdAction { get; }
    int ForecastGreen { get; }
    string ForecastGreenText { get; }
    float? BurstFinishIn { get; }
    float? BurstCommunioIn { get; }
    string FallbackReason { get; }
    string ProjectionDebug { get; }
    IEnumerable<string> RecentActions { get; }
    ReaperBurstRoute RecoveryRoute { get; }
    bool IsPlanned { get; }
    bool IsOpener { get; }
    bool IsCoordinating { get; }
    bool IsSimple { get; }
    bool RulesDisabled { get; }
    bool DeferSliceQueue { get; }
    bool DeferHarvestQueue { get; }
    bool CanWaitForHarvest { get; }
    void Update(ReaperState state, bool evaluateResources = false);
    void EnqueueAction(ulong source, uint id, uint sequence);
    void ObserveAction(uint id);
    void Reset(string reason = "重置");
    void Cancel(string reason);
    void Replan(string reason);
    void WindowChanged(string reason);
    void DisableRules();
    void NoteDumpFallback(uint id, bool off);
    void BeginOpener(ReaperState state);
    void NoteOpenerBlocked(uint id, uint? nativeStatus);
    bool HoldHarvest(long now, string unavailable = "");
    void StopHarvestWait(string reason, bool failed = false);
    bool AllowsDuringHarvestWait(uint id, long now);
    bool Allows(uint id);
    bool AllowsResource(uint id, bool fallback = false);
    IEnumerable<uint> DumpCandidates(ReaperState s, bool off) => ReaperDumpPlanner.Candidates(s, off);
    bool CanDump(ReaperState s, uint id, bool off) => ReaperDumpPlanner.CanUse(s, id, off);
}

// 100级只接入统一入口，原有算法及其内部调用保持原样。
public sealed partial class ReaperBurstPlanner : IReaperPlanner
{
    ReaperBurstRoute IReaperPlanner.RecoveryRoute => RecoveryRoute;
    bool IReaperPlanner.CanWaitForHarvest => CanWaitForHarvest;
    bool IReaperPlanner.HoldHarvest(long now, string unavailable) => HoldHarvest(now, unavailable);
    void IReaperPlanner.StopHarvestWait(string reason, bool failed) => StopHarvestWait(reason, failed);
    bool IReaperPlanner.AllowsDuringHarvestWait(uint id, long now) => AllowsDuringHarvestWait(id, now);
    void IReaperPlanner.NoteOpenerBlocked(uint id, uint? status) => NoteOpenerBlocked(id, status);
}
