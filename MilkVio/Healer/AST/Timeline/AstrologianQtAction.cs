using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Timeline.Core;

namespace MilkVio.Healer.AST.Timeline;

public sealed class AstrologianQtAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private const string TypeKey = "mioast_qt";

    private const string FieldQt = "qt";
    private const string FieldEnabled = "enabled";

    private string _qt = HealerQt.停手;
    private bool _enabled = true;

    public string NodeDisplayName => "占星QT控制";

    public NodeParamInfo[] Params =>
    [
        new(
            FieldQt,
            "QT",
            "选择要开关的占星行为。",
            "select",
            [
                (HealerQt.启用起手, HealerQt.启用起手),
                (HealerQt.停手, HealerQt.停手),
                (HealerQt.爆发药, HealerQt.爆发药),
                (HealerQt.AOE, HealerQt.AOE),
                (HealerQt.DOT, HealerQt.DOT),
                (HealerQt.醒梦, HealerQt.醒梦),
                (HealerQt.占卜, HealerQt.占卜),
                (HealerQt.抽卡, HealerQt.抽卡),
                (HealerQt.发卡, HealerQt.发卡),
                (HealerQt.剑卡, HealerQt.剑卡),
                (HealerQt.冠卡, HealerQt.冠卡),
                (HealerQt.光速, HealerQt.光速),
                (HealerQt.倒计时地星, HealerQt.倒计时地星),
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
            var action = new AstrologianQtAction();
            if (dto.Params == null) return action;

            if (dto.Params.TryGetValue(FieldQt, out var qt))
                action._qt = qt;
            if (dto.Params.TryGetValue(FieldEnabled, out var enabled))
                action._enabled = bool.TryParse(enabled, out var parsed) && parsed;

            return action;
        });
    }
}
