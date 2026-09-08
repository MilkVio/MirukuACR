using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Viper.Timeline;

public sealed class ViperGaugeCondition : ICondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string TypeKey = "vipergauge";
    private const string FieldComparison = "comparison";
    private const string FieldValue = "value";
    private const string CompareGreater = "gt";
    private const string CompareLess = "lt";
    private const string CompareGreaterOrEqual = "gte";
    private const string CompareLessOrEqual = "lte";
    private const string CompareEqual = "eq";

    private string _comparison = CompareGreaterOrEqual;
    private int _value = 50;

    public string NodeDisplayName => "灵力值检测";

    public NodeParamInfo[] Params =>
    [
        new(
            FieldComparison,
            "比较",
            "灵力值与设定值的比较方式",
            "select",
            [
                (CompareGreater, ">"),
                (CompareLess, "<"),
                (CompareGreaterOrEqual, ">="),
                (CompareLessOrEqual, "<="),
                (CompareEqual, "="),
            ]),
        new(FieldValue, "设定值", "用于比较的灵力值", "int"),
    ];

    public string GetParam(string fieldName) => fieldName switch
    {
        FieldComparison => _comparison,
        FieldValue => _value.ToString(),
        _ => string.Empty,
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case FieldComparison:
                _comparison = value;
                break;
            case FieldValue when int.TryParse(value, out var parsed):
                _value = Math.Clamp(parsed, 0, 100);
                break;
        }
    }

    public bool EvaluateImmediate()
        => Compare(JobGaugeHelper.VPR.灵力值, _value, _comparison);

    public bool EvaluateWait() => EvaluateImmediate();

    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string>
        {
            [FieldComparison] = _comparison,
            [FieldValue] = _value.ToString(),
        },
    };

    private static bool Compare(int left, int right, string comparison)
        => comparison switch
        {
            CompareGreater => left > right,
            CompareLess => left < right,
            CompareLessOrEqual => left <= right,
            CompareEqual => left == right,
            _ => left >= right,
        };

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, TypeKey, dto =>
        {
            var condition = new ViperGaugeCondition();
            if (dto.Params == null) return condition;

            if (dto.Params.TryGetValue(FieldComparison, out var comparison))
                condition._comparison = comparison;
            if (dto.Params.TryGetValue(FieldValue, out var value) && int.TryParse(value, out var parsed))
                condition._value = Math.Clamp(parsed, 0, 100);

            return condition;
        });
    }
}
