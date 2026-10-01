using ECommons.ExcelServices;
using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Machinist.Timeline;

public sealed class MachinistGaugeCondition : ICondition, IImmediateCondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string BatteryType = "milkviomachinistbattery";
    private const string HeatType = "milkviomachinistheat";
    private readonly bool _heat;
    private string _comparison = "gte";
    private int _value = 50;

    public MachinistGaugeCondition(bool heat) => _heat = heat;

    public string NodeDisplayName => _heat ? "热量" : "电量";
    public NodeParamInfo[] Params =>
    [
        new("comparison", "比较符", $"当前{NodeDisplayName}与设定值的比较方式", "enum",
            [("gt", ">"), ("gte", ">="), ("eq", "="), ("ne", "!="), ("lte", "<="), ("lt", "<")]),
        new("value", "设定值", "0–100，满足比较条件时成立", "int")
    ];

    public string GetParam(string fieldName) => fieldName switch
    {
        "comparison" => _comparison,
        "value" => _value.ToString(),
        _ => ""
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case "comparison": _comparison = value; break;
            case "value" when int.TryParse(value, out var parsed): _value = Math.Clamp(parsed, 0, 100); break;
        }
    }

    public bool EvaluateImmediate()
    {
        // 不依赖目标、战斗或QT；无角色/其他职业时不把缺失量谱当成0来触发条件。
        if (Core.Me == null || Core.Me.ClassJob.RowId != (uint)Job.MCH) return false;
        var current = _heat ? JobGaugeHelper.MCH.Heat : JobGaugeHelper.MCH.Battery;
        return _comparison switch
        {
            "gt" => current > _value,
            "gte" => current >= _value,
            "eq" => current == _value,
            "ne" => current != _value,
            "lte" => current <= _value,
            "lt" => current < _value,
            _ => false
        };
    }

    public bool EvaluateWait() => EvaluateImmediate();

    public ConditionDto ToDto() => new()
    {
        Type = _heat ? HeatType : BatteryType,
        Params = new Dictionary<string, string>
        {
            ["comparison"] = _comparison,
            ["value"] = _value.ToString()
        }
    };

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, BatteryType, dto => FromDto(dto, heat: false));
        ConditionFactory.Register(context, HeatType, dto => FromDto(dto, heat: true));
    }

    private static MachinistGaugeCondition FromDto(ConditionDto dto, bool heat)
    {
        var condition = new MachinistGaugeCondition(heat);
        if (dto.Params != null)
            foreach (var (key, value) in dto.Params) condition.SetParam(key, value);
        return condition;
    }
}
