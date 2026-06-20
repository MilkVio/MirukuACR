using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Timeline.Core;

namespace MilkVio.Healer.SGE.Timeline;

public sealed class SageQtAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "miosge_qt";

    private const string FieldQt = "qt";
    private const string FieldEnabled = "enabled";

    private string _qt = HealerQt.停手;
    private bool _enabled = true;

    public string NodeDisplayName => "贤者QT控制";

    public NodeParamInfo[] Params =>
    [
        new(
            FieldQt,
            "QT",
            "选择要开关的贤者行为。",
            "select",
            [
                (HealerQt.启用起手, HealerQt.启用起手),
                (HealerQt.停手, HealerQt.停手),
                (HealerQt.AOE, HealerQt.AOE),
                (HealerQt.DOT, HealerQt.DOT),
                (HealerQt.康复, HealerQt.康复),
                (HealerQt.醒梦, HealerQt.醒梦),
                (HealerQt.发炎, HealerQt.发炎),
                (HealerQt.箭毒, HealerQt.箭毒),
                (HealerQt.心神风息, HealerQt.心神风息),
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
            var action = new SageQtAction();
            if (dto.Params == null) return action;

            if (dto.Params.TryGetValue(FieldQt, out var qt))
                action._qt = qt;
            if (dto.Params.TryGetValue(FieldEnabled, out var rawEnabled))
                action._enabled = bool.TryParse(rawEnabled, out var parsedEnabled) && parsedEnabled;

            return action;
        });
    }
}
