using MilkVio.Healer.Common;
using PromeRotation.Timeline.Core;

namespace MilkVio.Healer.SCH.Timeline;

public sealed class ScholarGaugeCondition : ICondition, IImmediateCondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string TypeKey = "miosch_gauge";

    private const string FieldGauge = "gauge";
    private const string FieldComparison = "comparison";
    private const string FieldValue = "value";

    private const string GaugeAetherflow = "aetherflow";
    private const string GaugeFairyGauge = "fairy_gauge";
    private const string GaugeSeraphTimer = "seraph_timer";
    private const string GaugeHasPet = "has_pet";

    private const string CompareLess = "lt";
    private const string CompareLessOrEqual = "lte";
    private const string CompareEqual = "eq";
    private const string CompareGreaterOrEqual = "gte";
    private const string CompareGreater = "gt";

    private string _gauge = GaugeAetherflow;
    private string _comparison = CompareGreaterOrEqual;
    private int _value = 1;

    public string NodeDisplayName => "学者资源检测";

    public NodeParamInfo[] Params =>
    [
        new(
            FieldGauge,
            "资源",
            "选择要检测的学者资源。朝日存在按 1=true / 0=false 处理。",
            "select",
            [
                (GaugeAetherflow, "以太超流层数"),
                (GaugeFairyGauge, "异想以太"),
                (GaugeSeraphTimer, "炽天使剩余时间"),
                (GaugeHasPet, "朝日是否在场"),
            ]),
        new(
            FieldComparison,
            "比较",
            "资源值与阈值的比较方式。",
            "select",
            [
                (CompareLess, "<"),
                (CompareLessOrEqual, "<="),
                (CompareEqual, "=="),
                (CompareGreaterOrEqual, ">="),
                (CompareGreater, ">"),
            ]),
        new(FieldValue, "阈值", "用于比较的整数阈值。", "int"),
    ];

    public string GetParam(string fieldName) => fieldName switch
    {
        FieldGauge => _gauge,
        FieldComparison => _comparison,
        FieldValue => _value.ToString(),
        _ => string.Empty,
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case FieldGauge:
                _gauge = value;
                break;
            case FieldComparison:
                _comparison = value;
                break;
            case FieldValue when int.TryParse(value, out var parsed):
                _value = Math.Max(0, parsed);
                break;
        }
    }

    public bool EvaluateImmediate()
        => Compare(ReadGaugeValue(), _value, _comparison);

    public bool EvaluateWait() => EvaluateImmediate();

    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string>
        {
            [FieldGauge] = _gauge,
            [FieldComparison] = _comparison,
            [FieldValue] = _value.ToString(),
        },
    };

    private int ReadGaugeValue()
        => _gauge switch
        {
            GaugeAetherflow => HealerUtils.SchAetherflow(),
            GaugeFairyGauge => HealerUtils.SchFairyGauge(),
            GaugeSeraphTimer => HealerUtils.SchSeraphTimer(),
            GaugeHasPet => HealerUtils.SchHasPet() == true ? 1 : 0,
            _ => 0,
        };

    private static bool Compare(int left, int right, string comparison)
        => comparison switch
        {
            CompareLess => left < right,
            CompareLessOrEqual => left <= right,
            CompareEqual => left == right,
            CompareGreater => left > right,
            _ => left >= right,
        };

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, TypeKey, dto =>
        {
            var condition = new ScholarGaugeCondition();
            if (dto.Params == null) return condition;

            if (dto.Params.TryGetValue(FieldGauge, out var gauge))
                condition._gauge = gauge;
            if (dto.Params.TryGetValue(FieldComparison, out var comparison))
                condition._comparison = comparison;
            if (dto.Params.TryGetValue(FieldValue, out var rawValue) && int.TryParse(rawValue, out var parsedValue))
                condition._value = Math.Max(0, parsedValue);

            return condition;
        });
    }
}
