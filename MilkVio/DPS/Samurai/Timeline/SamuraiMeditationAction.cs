using System.Globalization;
using Dalamud.Bindings.ImGui;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Samurai.Timeline;

public sealed class SamuraiMeditationAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "milkviosammeditation";
    private SamuraiMeditationMode _mode;
    private string _comparison = "lte";
    private float _hp = 1;
    private static readonly (string, string)[] Modes = [("Always", "无条件"), ("NoEnemies", "等待无可选中敌人"), ("TargetHp", "等待当前目标血量")];
    private static readonly (string, string)[] Comparisons = [("gt", ">"), ("gte", ">="), ("eq", "="), ("ne", "!="), ("lte", "<="), ("lt", "<")];
    public string NodeDisplayName => "默想";
    public NodeParamInfo[] Params =>
    [
        new("mode", "开启条件", "无敌人检测范围100米", "enum", Modes),
        new("comparison", "比较符", "", "enum", Comparisons),
        new("hp", "血量（%）", "0～100", "float")
    ];
    public string GetParam(string fieldName) => fieldName switch
    {
        "mode" => _mode.ToString(), "comparison" => _comparison,
        "hp" => _hp.ToString(CultureInfo.InvariantCulture), _ => ""
    };
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "mode" && Enum.TryParse<SamuraiMeditationMode>(value, out var mode) && Enum.IsDefined(mode)) _mode = mode;
        if (fieldName == "comparison" && Comparisons.Any(p => p.Item1 == value)) _comparison = value;
        if (fieldName == "hp" && float.TryParse(value, CultureInfo.InvariantCulture, out var hp) && float.IsFinite(hp)) _hp = Math.Clamp(hp, 0, 100);
    }
    public void Execute() => SamuraiMeditation.Request(_mode, _comparison, _hp);
    public bool DrawJobNodeEditor()
    {
        if (ImGui.BeginCombo("开启条件", Modes[(int)_mode].Item2))
        {
            foreach (var (key, label) in Modes)
                if (ImGui.Selectable(label, key == _mode.ToString())) SetParam("mode", key);
            ImGui.EndCombo();
        }
        if (_mode == SamuraiMeditationMode.NoEnemies) ImGui.TextDisabled("周围100米");
        if (_mode != SamuraiMeditationMode.TargetHp) return true;
        if (ImGui.BeginCombo("比较符", Comparisons.First(p => p.Item1 == _comparison).Item2))
        {
            foreach (var (key, label) in Comparisons)
                if (ImGui.Selectable(label, key == _comparison)) _comparison = key;
            ImGui.EndCombo();
        }
        if (ImGui.InputFloat("血量（%）", ref _hp)) _hp = float.IsFinite(_hp) ? Math.Clamp(_hp, 0, 100) : 1;
        return true;
    }
    public ActionDto ToDto() => new() { Type = TypeKey, Params = new() { ["mode"] = GetParam("mode"), ["comparison"] = _comparison, ["hp"] = GetParam("hp") } };
    public static void Register(RotationNodeContext context) => ActionFactory.Register(context, TypeKey, dto =>
    {
        var action = new SamuraiMeditationAction();
        if (dto.Params != null) foreach (var pair in dto.Params) action.SetParam(pair.Key, pair.Value);
        return action;
    });
}
