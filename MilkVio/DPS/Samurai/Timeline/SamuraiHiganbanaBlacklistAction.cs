using System.Globalization;
using PromeRotation.PureTimeline.Runtime;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Samurai.Timeline;

public sealed class SamuraiHiganbanaBlacklistAction(bool clear = false) : IAction, ISerializableAction, IJobNodeDescriptor, IRuntimeContextAware
{
    private uint _baseId;
    private string TypeKey => clear ? "milkviosamclearhiganbana" : "milkviosamblockhiganbana";
    public string NodeDisplayName => clear ? "清除彼岸花黑名单" : "添加彼岸花黑名单";
    public object? RuntimeContext { get; set; }
    public NodeParamInfo[] Params => clear ? [] : [new("baseId", "BaseId", "", "int")];
    public string GetParam(string fieldName) => fieldName == "baseId" ? _baseId.ToString(CultureInfo.InvariantCulture) : "";
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "baseId")
            _baseId = uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0;
    }
    public void Execute()
    {
        if (clear) { SamuraiHiganbanaBlacklist.Clear("时间轴主动清除"); return; }
        // 使用旧SDK已有的公共接口，兼容当前宿主。
        var runtime = (RuntimeContext as IPtlEntryContext)?.Runtime ?? TimelineEngine.Instance.CurrentRuntime;
        SamuraiHiganbanaBlacklist.Add(_baseId, runtime?.Blackboard);
    }
    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = clear ? new() : new() { ["baseId"] = GetParam("baseId") }
    };
    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, "milkviosamblockhiganbana", dto =>
        {
            var action = new SamuraiHiganbanaBlacklistAction();
            if (dto.Params != null) foreach (var pair in dto.Params) action.SetParam(pair.Key, pair.Value);
            return action;
        });
        ActionFactory.Register(context, "milkviosamclearhiganbana", _ => new SamuraiHiganbanaBlacklistAction(true));
    }
}
