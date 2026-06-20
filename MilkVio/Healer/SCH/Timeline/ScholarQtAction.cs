using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Timeline.Core;

namespace MilkVio.Healer.SCH.Timeline;

public sealed class ScholarQtAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "miosch_qt";

    private const string FieldQt = "qt";
    private const string FieldEnabled = "enabled";

    private string _qt = HealerQt.停手;
    private bool _enabled = true;

    public string NodeDisplayName => "学者QT控制";

    public NodeParamInfo[] Params =>
    [
        new(
            FieldQt,
            "QT",
            "选择要开关的学者行为。",
            "select",
            [
                (HealerQt.启用起手, HealerQt.启用起手),
                (HealerQt.停手, HealerQt.停手),
                (HealerQt.爆发药, HealerQt.爆发药),
                (HealerQt.AOE, HealerQt.AOE),
                (HealerQt.DOT, HealerQt.DOT),
                (HealerQt.毁2, HealerQt.毁2),
                (HealerQt.康复, HealerQt.康复),
                (HealerQt.朝日召唤, HealerQt.朝日召唤),
                (HealerQt.以太超流, HealerQt.以太超流),
                (HealerQt.转化, HealerQt.转化),
                (HealerQt.能量吸收, HealerQt.能量吸收),
                (HealerQt.醒梦, HealerQt.醒梦),
                (HealerQt.连环计, HealerQt.连环计),
                (HealerQt.即刻极炎法, HealerQt.即刻极炎法),
            ]),
        new(FieldEnabled, "启用", "勾选为开启，取消勾选为关闭。", "bool"),
    ];

    public string GetParam(string fieldName) => fieldName switch
    {
        FieldQt => _qt,
        FieldEnabled => _enabled.ToString(),
        _ => string.Empty,
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case FieldQt:
                _qt = value;
                break;
            case FieldEnabled:
                _enabled = bool.TryParse(value, out var parsed) && parsed;
                break;
        }
    }

    public void Execute()
    {
        PromeSettings.Instance.SetQt(_qt, _enabled);
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string>
        {
            [FieldQt] = _qt,
            [FieldEnabled] = _enabled.ToString(),
        },
    };

    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, TypeKey, dto =>
        {
            var action = new ScholarQtAction();
            if (dto.Params == null) return action;

            if (dto.Params.TryGetValue(FieldQt, out var qt))
                action._qt = qt;
            if (dto.Params.TryGetValue(FieldEnabled, out var rawEnabled))
                action._enabled = bool.TryParse(rawEnabled, out var parsedEnabled) && parsedEnabled;

            return action;
        });
    }
}
