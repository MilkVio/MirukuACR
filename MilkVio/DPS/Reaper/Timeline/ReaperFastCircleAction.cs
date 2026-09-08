using Dalamud.Bindings.ImGui;
using MilkVio.DPS.Reaper.ReaperData;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Reaper.Timeline;

public sealed class ReaperFastCircleAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "milkvioreaperfastcircle";
    private bool _enabled;
    public string NodeDisplayName => "快速神秘环";
    public NodeParamInfo[] Params => [new("enabled", "快速神秘环", "烙印后尽早开环，战斗结束恢复关闭", "bool")];
    public string GetParam(string fieldName) => fieldName == "enabled" ? _enabled.ToString() : "";
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "enabled" && bool.TryParse(value, out var enabled)) _enabled = enabled;
    }
    public void Execute() => ReaperBattleData.Instance.SetFastCircle(_enabled);
    public bool DrawJobNodeEditor()
    {
        ImGui.Checkbox("快速神秘环", ref _enabled);
        return true;
    }
    public ActionDto ToDto() => new() { Type = TypeKey, Params = new() { ["enabled"] = _enabled.ToString() } };
    public static void Register(RotationNodeContext context) => ActionFactory.Register(context, TypeKey, dto =>
    {
        var action = new ReaperFastCircleAction();
        if (dto.Params != null) foreach (var pair in dto.Params) action.SetParam(pair.Key, pair.Value);
        return action;
    });
}
