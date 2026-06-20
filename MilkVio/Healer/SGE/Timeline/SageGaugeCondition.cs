using MilkVio.Healer.Common;
using PromeRotation.Timeline.Core;

namespace MilkVio.Healer.SGE.Timeline;

public sealed class SageGaugeCondition : ICondition, IImmediateCondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string TypeKey = "miosge_gauge";

    private const string FieldGauge = "gauge";
    private const string FieldComparison = "comparison";
    private const string FieldValue = "value";

    private const string GaugeAddersgall = "addersgall";
    private const string GaugeAddersting = "addersting";
    private const string GaugeEukrasia = "eukrasia";
    private const string GaugeAddersgallTimer = "addersgall_timer";

    private const string CompareLess = "lt";
    private const string CompareLessOrEqual = "lte";
    private const string CompareEqual = "eq";
    private const string CompareGreaterOrEqual = "gte";
    private const string CompareGreater = "gt";

    private string _gauge = GaugeAddersgall;
    private string _comparison = CompareGreaterOrEqual;
    private int _value = 1;

    public string NodeDisplayName => "贤者资源检测";

    public NodeParamInfo[] Params =>
    [
        new(
            FieldGauge,
            "资源",
            "选择要检测的贤者资源。均衡按 1=true / 0=false 处理。",
            "select",
            [
                (GaugeAddersgall, "蛇胆 Addersgall"),
                (GaugeAddersting, "蛇刺 Addersting"),
                (GaugeEukrasia, "均衡 Eukrasia"),
                (GaugeAddersgallTimer, "蛇胆计时"),
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

    private static int ReadGaugeValue(string gauge)
        => gauge switch
        {
            GaugeAddersgall => HealerUtils.SgeAddersgall(),
            GaugeAddersting => HealerUtils.SgeAddersting(),
            GaugeEukrasia => HealerUtils.SgeEukrasia() ? 1 : 0,
            GaugeAddersgallTimer => HealerUtils.SgeAddersgallTimer(),
            _ => 0,
        };

    private int ReadGaugeValue() => ReadGaugeValue(_gauge);

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
            var condition = new SageGaugeCondition();
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
