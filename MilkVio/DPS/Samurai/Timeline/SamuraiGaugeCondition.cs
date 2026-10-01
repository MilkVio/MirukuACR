using ECommons.ExcelServices;
using MilkVio.DPS.Samurai.SAMData;
using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Samurai.Timeline;

public sealed class SamuraiGaugeCondition : ICondition, IImmediateCondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string SenType = "milkviosamsen";
    private const string MirrorType = "milkviosammirrorcharges";
    private readonly bool _mirror;
    private string _comparison = "gte";
    private int _value = 1;

    public SamuraiGaugeCondition(bool mirror) => _mirror = mirror;
    public string NodeDisplayName => _mirror ? "明镜层数" : "闪数量";
    public NodeParamInfo[] Params =>
    [
        new("comparison", "比较符", $"当前{NodeDisplayName}与设定值的比较方式", "enum",
            [("gt", ">"), ("gte", ">="), ("eq", "="), ("ne", "!="), ("lte", "<="), ("lt", "<")]),
        new("value", "设定值", _mirror ? "技能可用充能0～2，不是明镜增益层数" : "雪、月、花当前共持有0～3闪", "int")
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
            case "value" when int.TryParse(value, out var parsed): _value = Math.Clamp(parsed, 0, _mirror ? 2 : 3); break;
        }
    }
    public bool EvaluateImmediate()
    {
        if (Core.Me == null || Core.Me.ClassJob.RowId != (uint)Job.SAM) return false;
        var current = JobGaugeHelper.SAM.GetSenCount();
        if (_mirror) current = ActionHelper.IsActionAvailableByLevelAndQuest(SAMSkill.明镜止水) ? (int)SamuraiHelper.明镜止水层数() : 0;
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
        Type = _mirror ? MirrorType : SenType,
        Params = new() { ["comparison"] = _comparison, ["value"] = _value.ToString() }
    };
    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, SenType, dto => FromDto(dto, false));
        ConditionFactory.Register(context, MirrorType, dto => FromDto(dto, true));
    }
    private static SamuraiGaugeCondition FromDto(ConditionDto dto, bool mirror)
    {
        var condition = new SamuraiGaugeCondition(mirror);
        if (dto.Params != null) foreach (var pair in dto.Params) condition.SetParam(pair.Key, pair.Value);
        return condition;
    }
}
