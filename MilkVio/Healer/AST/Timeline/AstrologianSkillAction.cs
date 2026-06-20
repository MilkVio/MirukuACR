using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Managers;
using PromeRotation.Resolvers;
using PromeRotation.Timeline.Core;
using PromeRotation.Updaters;

namespace MilkVio.Healer.AST.Timeline;

public sealed class AstrologianSkillAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private sealed record SkillDef(string Key, string Label, uint ActionId, ActionType Type, ActionTargetType DefaultTarget);

    private const string TypeKey = "mioast_skill";

    private const string FieldSkill = "skill";
    private const string FieldTarget = "target";
    private const string FieldMode = "mode";
    private const string FieldHighPriority = "high_priority";

    private const string TargetDefault = "Default";
    private const string ModeQueue = "queue";
    private const string ModeForce = "force";

    private static readonly SkillDef[] Skills =
    [
        new("malefic", "凶星 / 落陷凶星", AstAction.凶星, ActionType.Gcd, ActionTargetType.Target),
        new("combust", "焚灼", AstAction.焚灼, ActionType.Gcd, ActionTargetType.Target),
        new("gravity", "重力", AstAction.重力, ActionType.Gcd, ActionTargetType.Target),
        new("aspected_benefic", "吉星相位", AstAction.吉星相位, ActionType.Gcd, ActionTargetType.Self),
        new("benefic_ii", "福星", AstAction.福星, ActionType.Gcd, ActionTargetType.Self),
        new("helios", "阳星", AstAction.阳星, ActionType.Gcd, ActionTargetType.Self),
        new("ascend", "生辰", AstAction.生辰, ActionType.Gcd, ActionTargetType.Self),
        new("esuna", "康复", RoleAction.康复, ActionType.Gcd, ActionTargetType.Self),
        new("lucid", "醒梦", RoleAction.醒梦, ActionType.OffGcd, ActionTargetType.Self),
        new("lightspeed", "光速", AstAction.光速, ActionType.OffGcd, ActionTargetType.Self),
        new("divination", "占卜", AstAction.占卜, ActionType.OffGcd, ActionTargetType.Self),
        new("oracle", "神谕", AstAction.神谕, ActionType.OffGcd, ActionTargetType.Target),
        new("astral_draw", "星极抽卡", AstAction.星极抽卡, ActionType.OffGcd, ActionTargetType.Self),
        new("umbral_draw", "灵极抽卡", AstAction.灵极抽卡, ActionType.OffGcd, ActionTargetType.Self),
        new("play_balance", "出卡太阳", AstAction.出卡太阳, ActionType.OffGcd, ActionTargetType.Self),
        new("play_spear", "出卡月亮", AstAction.出卡月亮, ActionType.OffGcd, ActionTargetType.Self),
        new("lord", "王冠之领主", AstAction.王冠之领主, ActionType.OffGcd, ActionTargetType.Self),
        new("lady", "王冠之贵妇", AstAction.王冠之贵妇, ActionType.OffGcd, ActionTargetType.Self),
        new("essential_dignity", "擢升", AstAction.擢升, ActionType.OffGcd, ActionTargetType.Self),
        new("celestial_intersection", "天星交错", AstAction.天星交错, ActionType.OffGcd, ActionTargetType.Self),
        new("earthly_star", "地星", AstAction.地星, ActionType.OffGcd, ActionTargetType.Self),
        new("horoscope", "天宫图", AstAction.天宫图, ActionType.OffGcd, ActionTargetType.Self),
        new("neutral_sect", "中间学派", AstAction.中间学派, ActionType.OffGcd, ActionTargetType.Self),
        new("sun_sign", "太阳星座", AstAction.太阳星座, ActionType.OffGcd, ActionTargetType.Self),
    ];

    private string _skill = "divination";
    private string _target = TargetDefault;
    private string _mode = ModeQueue;
    private bool _highPriority = true;

    public string NodeDisplayName => "占星技能执行";

    public NodeParamInfo[] Params =>
    [
        new(
            FieldSkill,
            "技能",
            "选择要由时间轴执行的占星技能。",
            "select",
            Skills.Select(skill => (skill.Key, skill.Label)).ToArray()),
        new(
            FieldTarget,
            "目标",
            "默认目标会使用该技能的推荐目标；需要定点治疗或发卡时可改成指定队员。",
            "select",
            [
                (TargetDefault, "默认"),
                (nameof(ActionTargetType.Self), "自己"),
                (nameof(ActionTargetType.Target), "当前目标"),
                (nameof(ActionTargetType.TargetOfTarget), "目标的目标"),
                (nameof(ActionTargetType.FocusTarget), "焦点目标"),
                (nameof(ActionTargetType.MouseOver), "鼠标悬停"),
                (nameof(ActionTargetType.LowestHealthPartyMember), "最低血量队员"),
                (nameof(ActionTargetType.PartyMember2), "小队2"),
                (nameof(ActionTargetType.PartyMember3), "小队3"),
                (nameof(ActionTargetType.PartyMember4), "小队4"),
                (nameof(ActionTargetType.PartyMember5), "小队5"),
                (nameof(ActionTargetType.PartyMember6), "小队6"),
                (nameof(ActionTargetType.PartyMember7), "小队7"),
                (nameof(ActionTargetType.PartyMember8), "小队8"),
            ]),
        new(
            FieldMode,
            "执行方式",
            "入队会交给 Prome 队列处理；强制使用会立即调用 UseAction。",
            "select",
            [
                (ModeQueue, "入队"),
                (ModeForce, "强制使用"),
            ]),
        new(FieldHighPriority, "高优先级", "只对入队方式生效。", "bool"),
    ];

    public string GetParam(string fieldName) => fieldName switch
    {
        FieldSkill => _skill,
        FieldTarget => _target,
        FieldMode => _mode,
        FieldHighPriority => _highPriority.ToString(),
        _ => string.Empty,
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case FieldSkill:
                _skill = value;
                break;
            case FieldTarget:
                _target = value;
                break;
            case FieldMode:
                _mode = value;
                break;
            case FieldHighPriority:
                _highPriority = bool.TryParse(value, out var parsed) && parsed;
                break;
        }
    }

    public void Execute()
    {
        var skill = Skills.FirstOrDefault(skill => skill.Key == _skill) ?? Skills[0];
        var target = ResolveTarget(skill);
        var action = CreateAction(skill, target);

        if (_mode == ModeForce)
            ActionUpdater.UseAction(action);
        else
            ActionQueueManager.Enqueue(action, _highPriority);
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string>
        {
            [FieldSkill] = _skill,
            [FieldTarget] = _target,
            [FieldMode] = _mode,
            [FieldHighPriority] = _highPriority.ToString(),
        },
    };

    private ActionTargetType ResolveTarget(SkillDef skill)
    {
        if (_target == TargetDefault)
            return skill.DefaultTarget;

        return Enum.TryParse<ActionTargetType>(_target, true, out var parsed)
            ? parsed
            : skill.DefaultTarget;
    }

    private static PAction CreateAction(SkillDef skill, ActionTargetType target)
    {
        if (skill.ActionId == AstAction.地星)
        {
            var positionTarget = TargetResolver.Resolve(target) ?? HealerUtils.Me;
            return new PAction(skill.ActionId, skill.Type, target)
            {
                IsLocationAction = true,
                Position = positionTarget?.Position ?? default,
            };
        }

        return skill.Type == ActionType.Gcd
            ? HealerUtils.Gcd(skill.ActionId, target)
            : HealerUtils.OffGcd(skill.ActionId, target);
    }

    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, TypeKey, dto =>
        {
            var action = new AstrologianSkillAction();
            if (dto.Params == null) return action;

            if (dto.Params.TryGetValue(FieldSkill, out var skill))
                action._skill = skill;
            if (dto.Params.TryGetValue(FieldTarget, out var target))
                action._target = target;
            if (dto.Params.TryGetValue(FieldMode, out var mode))
                action._mode = mode;
            if (dto.Params.TryGetValue(FieldHighPriority, out var highPriority))
                action._highPriority = bool.TryParse(highPriority, out var parsed) && parsed;

            return action;
        });
    }
}
