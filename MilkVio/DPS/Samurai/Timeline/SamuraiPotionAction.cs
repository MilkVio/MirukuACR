using System.Globalization;
using Dalamud.Bindings.ImGui;
using MilkVio.DPS.Samurai.Level100;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Samurai.Timeline;

public sealed class SamuraiPotionAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "milkviosampotion";
    private float _seconds = 10;
    public string NodeDisplayName => "最优爆发药";
    public NodeParamInfo[] Params => [new("seconds", "允许等待（秒）", "0至30秒；0只在下一次自动选招时立即尝试，不能使用则结束", "float")];
    public string GetParam(string fieldName) => fieldName == "seconds" ? _seconds.ToString(CultureInfo.InvariantCulture) : "";
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "seconds" && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && float.IsFinite(seconds))
            _seconds = Math.Clamp(seconds, 0, 30);
    }
    public void Execute() => Samurai100Planning.RequestPotion(_seconds);
    public bool DrawJobNodeEditor()
    {
        if (ImGui.InputFloat("允许等待（秒）", ref _seconds))
            _seconds = float.IsFinite(_seconds) ? Math.Clamp(_seconds, 0, 30) : 10;
        return true;
    }
    public ActionDto ToDto() => new() { Type = TypeKey, Params = new() { ["seconds"] = GetParam("seconds") } };
    public static void Register(RotationNodeContext context) => ActionFactory.Register(context, TypeKey, dto =>
    {
        var action = new SamuraiPotionAction();
        if (dto.Params != null) foreach (var pair in dto.Params) action.SetParam(pair.Key, pair.Value);
        return action;
    });
}
