using Dalamud.Bindings.ImGui;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Samurai.Timeline;

public sealed class SamuraiForceMeikyoAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "milkviosamforcemeikyo";
    private bool _waitForCharge;
    public string NodeDisplayName => "强制请求一次明镜";
    public NodeParamInfo[] Params => [new("waitForCharge", "等待层数可用", "关闭且无充能时丢弃请求", "bool")];
    public string GetParam(string fieldName) => fieldName == "waitForCharge" ? _waitForCharge.ToString() : "";
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "waitForCharge" && bool.TryParse(value, out var wait)) _waitForCharge = wait;
    }
    public void Execute() => SamuraiTimeline.RequestMeikyo(_waitForCharge, true);
    public bool DrawJobNodeEditor()
    {
        ImGui.Checkbox("等待层数可用", ref _waitForCharge);
        return true;
    }
    public ActionDto ToDto() => new() { Type = TypeKey, Params = new() { ["waitForCharge"] = _waitForCharge.ToString() } };
    public static void Register(RotationNodeContext context) => ActionFactory.Register(context, TypeKey, dto =>
    {
        var action = new SamuraiForceMeikyoAction();
        if (dto.Params != null) foreach (var pair in dto.Params) action.SetParam(pair.Key, pair.Value);
        return action;
    });
}
