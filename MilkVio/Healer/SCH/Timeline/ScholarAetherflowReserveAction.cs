using MilkVio.Healer.Common;
using PromeRotation.Timeline.Core;

namespace MilkVio.Healer.SCH.Timeline;

public sealed class ScholarAetherflowReserveAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "miosch_aetherflow_reserve";

    private const string FieldReserve = "reserve";

    private int _reserve;

    public string NodeDisplayName => "学者豆子保留";

    public NodeParamInfo[] Params =>
    [
        new(FieldReserve, "保留层数", "能量吸收只会在以太超流层数高于此值时使用。", "int"),
    ];

    public string GetParam(string fieldName) => fieldName switch
    {
        FieldReserve => _reserve.ToString(),
        _ => string.Empty,
    };

    public void SetParam(string fieldName, string value)
    {
        if (fieldName == FieldReserve && int.TryParse(value, out var parsed))
            _reserve = Math.Clamp(parsed, 0, 3);
    }

    public void Execute()
    {
        MioAcrSettings.Instance.SchAetherflowReserve = Math.Clamp(_reserve, 0, 3);
        MioAcrSettings.Save();
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string>
        {
            [FieldReserve] = _reserve.ToString(),
        },
    };

    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, TypeKey, dto =>
        {
            var action = new ScholarAetherflowReserveAction();
            if (dto.Params == null) return action;

            if (dto.Params.TryGetValue(FieldReserve, out var rawReserve) && int.TryParse(rawReserve, out var reserve))
                action._reserve = Math.Clamp(reserve, 0, 3);

            return action;
        });
    }
}
