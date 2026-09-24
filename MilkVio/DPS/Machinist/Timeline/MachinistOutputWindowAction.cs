using System.Globalization;
using Dalamud.Bindings.ImGui;
using MilkVio.DPS.Machinist.Planning;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Machinist.Timeline;

public sealed class MachinistOutputWindowAction : IAction, ISerializableAction, IJobNodeDescriptor, IRuntimeContextAware
{
    private const string TypeKey = "milkviomachinistwindow";
    private MachinistWindowSettings _settings = new();
    private string _error = "";
    private MachinistWindowSettings? _validated;
    public object? RuntimeContext { get; set; }
    public string NodeDisplayName => "设置可输出窗口";
    public NodeParamInfo[] Params =>
    [
        new("seconds", "距离不可选中（秒）", "从节点执行开始计算", "float"),
        new("keep", "保留期末资源", "计时窗口在期末达标；事件/不可选中窗口在前窗口开始时达标并保持", "bool"),
        new("heat", "期末热量", "", "int"), new("battery", "期末电量", "", "int"),
        new("time", "到时间直接结束", "", "bool"), new("enemies", "Boss不可选中结束", "100米内无可攻击敌人", "bool"),
        new("event", "事件发生结束", "事件优先于不可选中，再优先于直接结束", "bool"),
        new("kind", "事件类型", "", "enum", [("0", "开始咏唱"), ("1", "技能效果"), ("2", "天气变化")]),
        new("match", "ActionId正则 / 天气ID", "例如31123|7892；天气填写单个ID", "string"),
        new("before", "前窗口（秒）", "提前开始检测", "float"), new("after", "后窗口（秒）", "到上限必须结束", "float")
    ];

    public string GetParam(string fieldName) => fieldName switch
    {
        "seconds" => _settings.Seconds.ToString(CultureInfo.InvariantCulture), "keep" => _settings.KeepResources.ToString(),
        "heat" => _settings.Heat.ToString(), "battery" => _settings.Battery.ToString(),
        "time" => _settings.EndOnTime.ToString(), "enemies" => _settings.EndWithoutEnemies.ToString(), "event" => _settings.EndOnEvent.ToString(),
        "kind" => ((int)_settings.Event).ToString(), "match" => _settings.Match,
        "before" => _settings.Before.ToString(CultureInfo.InvariantCulture), "after" => _settings.After.ToString(CultureInfo.InvariantCulture), _ => ""
    };

    public void SetParam(string fieldName, string value)
    {
        var number = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed);
        var integer = int.TryParse(value, out var n);
        var flag = bool.TryParse(value, out var b);
        _settings = fieldName switch
        {
            "seconds" when number => _settings with { Seconds = parsed }, "keep" when flag => _settings with { KeepResources = b },
            "heat" when integer => _settings with { Heat = Math.Clamp(n, 0, 100) }, "battery" when integer => _settings with { Battery = Math.Clamp(n, 0, 100) },
            "time" when flag => _settings with { EndOnTime = b }, "enemies" when flag => _settings with { EndWithoutEnemies = b },
            "event" when flag => _settings with { EndOnEvent = b }, "kind" when integer => _settings with { Event = (MachinistWindowEvent)n },
            "match" => _settings with { Match = value }, "before" when number => _settings with { Before = parsed },
            "after" when number => _settings with { After = parsed }, _ => _settings
        };
    }

    public void Execute()
    {
        // preview.7尚未导出统一上下文接口；TL/PTL均公开Runtime属性，仅节点执行时读取。
        var runtime = RuntimeContext as TimelineRuntime
            ?? RuntimeContext?.GetType().GetProperty("Runtime")?.GetValue(RuntimeContext) as TimelineRuntime;
        if (runtime == null) throw new InvalidOperationException("缺少当前时间轴运行上下文");
        if (!MachinistPlanning.Window.Set(_settings, runtime.Blackboard, Environment.TickCount64, out var error))
            throw new InvalidOperationException(error);
    }

    public bool DrawJobNodeEditor()
    {
        DrawFloat("seconds", "距离不可选中（秒）");
        DrawBool("keep", "保留期末资源");
        DrawInt("heat", "期末热量"); ImGui.SameLine(); DrawInt("battery", "期末电量");
        ImGui.Separator();
        DrawBool("time", "到时间直接结束");
        DrawBool("enemies", "Boss不可选中结束（周围无可攻击敌人）");
        DrawBool("event", "事件发生结束");
        ImGui.SameLine();
        var kind = (int)_settings.Event;
        ImGui.SetNextItemWidth(140);
        if (ImGui.Combo("##事件类型", ref kind, "开始咏唱\0技能效果\0天气变化\0")) _settings = _settings with { Event = (MachinistWindowEvent)kind };
        ImGui.SameLine();
        var match = _settings.Match; ImGui.SetNextItemWidth(180);
        if (ImGui.InputText("##结束匹配", ref match, 512)) _settings = _settings with { Match = match };
        DrawFloat("before", "前窗口（秒）"); ImGui.SameLine(); DrawFloat("after", "后窗口（秒）");
        ImGui.TextWrapped("比较窗口内可结算收益；不保留资源不等于立刻倾泻。结束方式优先级：事件 > 无可攻击敌人 > 计时。");
        if (_settings.KeepResources && (_settings.EndOnEvent || _settings.EndWithoutEnemies))
            ImGui.TextWrapped("资源将在前窗口开始时达标，并保持到实际结束；后窗口不作为保证的输出时间。");
        // 校验只使用临时实例，不向正在运行的时间轴发布设置。
        if (_validated != _settings)
        {
            var preview = new MachinistOutputWindow();
            preview.Set(_settings, null, 0, out _error);
            _validated = _settings;
        }
        if (_error.Length > 0) ImGui.TextUnformatted(_error);
        return true;
    }

    private void DrawFloat(string key, string label)
    {
        var value = float.Parse(GetParam(key), CultureInfo.InvariantCulture);
        ImGui.SetNextItemWidth(105);
        if (ImGui.InputFloat(label, ref value)) SetParam(key, value.ToString(CultureInfo.InvariantCulture));
    }
    private void DrawInt(string key, string label)
    {
        var value = int.Parse(GetParam(key)); ImGui.SetNextItemWidth(105);
        if (ImGui.InputInt(label, ref value)) SetParam(key, value.ToString());
    }
    private void DrawBool(string key, string label)
    {
        var value = bool.Parse(GetParam(key));
        if (ImGui.Checkbox(label, ref value)) SetParam(key, value.ToString());
    }

    public ActionDto ToDto() => new() { Type = TypeKey, Params = Params.ToDictionary(p => p.FieldName, p => GetParam(p.FieldName)) };
    public static void Register(RotationNodeContext context) => ActionFactory.Register(context, TypeKey, dto =>
    {
        var action = new MachinistOutputWindowAction();
        if (dto.Params != null) foreach (var pair in dto.Params) action.SetParam(pair.Key, pair.Value);
        return action;
    });
}

public sealed class MachinistClearWindowAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "milkviomachinistclearwindow";
    public string NodeDisplayName => "清除可输出窗口";
    public NodeParamInfo[] Params => [];
    public string GetParam(string fieldName) => "";
    public void SetParam(string fieldName, string value) { }
    public void Execute()
    {
        MachinistPlanning.Window.Clear();
    }
    public ActionDto ToDto() => new() { Type = TypeKey };
    public static void Register(RotationNodeContext context) => ActionFactory.Register(context, TypeKey, _ => new MachinistClearWindowAction());
}
