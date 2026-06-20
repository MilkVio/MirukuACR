using MilkVio.Healer.Common;
using PromeRotation.Timeline.Core;

namespace MilkVio.Healer.AST.Timeline;

public sealed class AstrologianGaugeCondition : ICondition, IImmediateCondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string TypeKey = "mioast_gauge";

    private const string FieldGauge = "gauge";
    private const string FieldValue = "value";

    private const string GaugeActiveDraw = "active_draw";
    private const string GaugeDrawnCard = "drawn_card";
    private const string GaugeCrownCard = "crown_card";

    private const string ValueAny = "Any";

    private string _gauge = GaugeDrawnCard;
    private string _value = ValueAny;

    public string NodeDisplayName => "占星卡牌检测";

    public NodeParamInfo[] Params =>
    [
        new(
            FieldGauge,
            "资源",
            "选择要检测的占星资源。",
            "select",
            [
                (GaugeActiveDraw, "当前抽卡极性"),
                (GaugeDrawnCard, "手牌"),
                (GaugeCrownCard, "王冠卡"),
            ]),
        new(
            FieldValue,
            "值",
            "Any 表示存在任意非 None 值；也可以选择具体卡牌或极性。",
            "select",
            [
                (ValueAny, "Any"),
                ("None", "None"),
                ("Astral", "Astral"),
                ("Umbral", "Umbral"),
                ("Balance", "Balance"),
                ("Spear", "Spear"),
                ("Arrow", "Arrow"),
                ("Bole", "Bole"),
                ("Ewer", "Ewer"),
                ("Spire", "Spire"),
                ("Lord", "Lord"),
                ("Lady", "Lady"),
            ]),
    ];

    public string GetParam(string fieldName) => fieldName switch
    {
        FieldGauge => _gauge,
        FieldValue => _value,
        _ => string.Empty,
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case FieldGauge:
                _gauge = value;
                break;
            case FieldValue:
                _value = value;
                break;
        }
    }

    public bool EvaluateImmediate()
    {
        var current = ReadGaugeValue();
        return _value == ValueAny
            ? !string.IsNullOrWhiteSpace(current) && current != "None"
            : string.Equals(current, _value, StringComparison.OrdinalIgnoreCase);
    }

    public bool EvaluateWait() => EvaluateImmediate();

    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string>
        {
            [FieldGauge] = _gauge,
            [FieldValue] = _value,
        },
    };

    private string ReadGaugeValue()
        => _gauge switch
        {
            GaugeActiveDraw => HealerUtils.AstActiveDrawText(),
            GaugeCrownCard => HealerUtils.AstDrawnCrownCard(),
            _ => HealerUtils.AstDrawnCard(),
        };

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, TypeKey, dto =>
        {
            var condition = new AstrologianGaugeCondition();
            if (dto.Params == null) return condition;

            if (dto.Params.TryGetValue(FieldGauge, out var gauge))
                condition._gauge = gauge;
            if (dto.Params.TryGetValue(FieldValue, out var value))
                condition._value = value;

            return condition;
        });
    }
}
