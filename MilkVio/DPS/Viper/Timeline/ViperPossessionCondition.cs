using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Viper.Timeline;

public sealed class ViperPossessionCondition : ICondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string TypeKey = "viperpossession";
    private const string FieldIsPossessed = "is_possessed";

    private bool _isPossessed = true;

    public string NodeDisplayName => "附体状态检测";

    public NodeParamInfo[] Params =>
    [
        new(FieldIsPossessed, "是否在附体", "勾选检测附体，取消检测未附体", "bool"),
    ];

    public string GetParam(string fieldName) => fieldName == FieldIsPossessed
        ? _isPossessed.ToString()
        : string.Empty;

    public void SetParam(string fieldName, string value)
    {
        if (fieldName == FieldIsPossessed)
            _isPossessed = bool.TryParse(value, out var parsed) && parsed;
    }

    public bool EvaluateImmediate() => ViperHelper.IsIn附体() == _isPossessed;

    public bool EvaluateWait() => EvaluateImmediate();

    public ConditionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string>
        {
            [FieldIsPossessed] = _isPossessed.ToString(),
        },
    };

    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, TypeKey, dto =>
        {
            var condition = new ViperPossessionCondition();
            if (dto.Params?.TryGetValue(FieldIsPossessed, out var value) == true)
                condition._isPossessed = bool.TryParse(value, out var parsed) && parsed;
            return condition;
        });
    }
}
