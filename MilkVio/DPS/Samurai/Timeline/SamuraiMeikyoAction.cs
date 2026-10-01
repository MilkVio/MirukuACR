using Dalamud.Bindings.ImGui;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Samurai.Timeline;

public sealed class SamuraiMeikyoAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "milkviosammeikyo";
    private bool _waitForCharge;
    public string NodeDisplayName => "请求一次明镜";
    public NodeParamInfo[] Params => [new("waitForCharge", "等待层数可用", "百级单体：开启后等待充能；关闭且当前无充能则报错并丢弃。绕过明镜QT，只请求一次", "bool")];
    public string GetParam(string fieldName) => fieldName == "waitForCharge" ? _waitForCharge.ToString() : "";
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "waitForCharge" && bool.TryParse(value, out var wait)) _waitForCharge = wait;
    }
    public void Execute() => SamuraiTimeline.RequestMeikyo(_waitForCharge);
    public bool DrawJobNodeEditor()
    {
        ImGui.Checkbox("等待层数可用", ref _waitForCharge);
        return true;
    }
    public ActionDto ToDto() => new() { Type = TypeKey, Params = new() { ["waitForCharge"] = _waitForCharge.ToString() } };
    public static void Register(RotationNodeContext context) => ActionFactory.Register(context, TypeKey, dto =>
    {
        var action = new SamuraiMeikyoAction();
        if (dto.Params != null) foreach (var pair in dto.Params) action.SetParam(pair.Key, pair.Value);
        return action;
    });
}
