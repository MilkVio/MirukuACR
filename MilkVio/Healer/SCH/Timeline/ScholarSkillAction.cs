using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Managers;
using PromeRotation.Timeline.Core;
using PromeRotation.Updaters;

namespace MilkVio.Healer.SCH.Timeline;

public sealed class ScholarSkillAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private sealed record SkillDef(string Key, string Label, uint ActionId, ActionType Type, ActionTargetType DefaultTarget);

    private const string TypeKey = "miosch_skill";

    private const string FieldSkill = "skill";
    private const string FieldTarget = "target";
    private const string FieldMode = "mode";
    private const string FieldHighPriority = "high_priority";

    private const string TargetDefault = "Default";
    private const string ModeQueue = "queue";
    private const string ModeForce = "force";

    private static readonly SkillDef[] Skills =
    [
        new("broil", "毁灭 / 极炎法", SchAction.毁灭, ActionType.Gcd, ActionTargetType.Target),
        new("bio", "毒菌 / 蛊毒法", SchAction.毒菌, ActionType.Gcd, ActionTargetType.Target),
        new("ruin_ii", "毁坏", SchAction.毁坏, ActionType.Gcd, ActionTargetType.Target),
        new("art_of_war", "破阵法 / 裂阵法", SchAction.破阵法, ActionType.Gcd, ActionTargetType.Self),
        new("summon_eos", "召唤朝日", SchAction.朝日召唤, ActionType.Gcd, ActionTargetType.Self),
        new("adloquium", "鼓舞激励之策", SchAction.鼓舞激励之策, ActionType.Gcd, ActionTargetType.Self),
        new("succor", "士气高扬之策", SchAction.士气高扬之策, ActionType.Gcd, ActionTargetType.Self),
        new("accession", "意气轩昂之策", SchAction.意气轩昂之策, ActionType.Gcd, ActionTargetType.Self),
        new("raise", "复生", SchAction.复生, ActionType.Gcd, ActionTargetType.Self),
        new("esuna", "康复", RoleAction.康复, ActionType.Gcd, ActionTargetType.Self),
        new("swiftcast", "即刻咏唱", RoleAction.即刻咏唱, ActionType.OffGcd, ActionTargetType.Self),
        new("lucid", "醒梦", RoleAction.醒梦, ActionType.OffGcd, ActionTargetType.Self),
        new("chain", "连环计", SchAction.连环计, ActionType.OffGcd, ActionTargetType.Target),
        new("baneful", "毒炎冲击", SchAction.毒炎冲击, ActionType.OffGcd, ActionTargetType.Target),
        new("aetherflow", "以太超流", SchAction.以太超流, ActionType.OffGcd, ActionTargetType.Self),
        new("energy_drain", "能量吸收", SchAction.能量吸收, ActionType.OffGcd, ActionTargetType.Target),
        new("dissipation", "转化", SchAction.转化, ActionType.OffGcd, ActionTargetType.Self),
        new("recitation", "秘策", SchAction.秘策, ActionType.OffGcd, ActionTargetType.Self),
        new("protraction", "生命回生法", SchAction.生命回生法, ActionType.OffGcd, ActionTargetType.Self),
        new("deployment", "展开战术", SchAction.展开战术, ActionType.OffGcd, ActionTargetType.Self),
        new("fey_illumination", "异想的幻光", SchAction.异想的幻光, ActionType.OffGcd, ActionTargetType.Self),
        new("fey_blessing", "异想的祥光", SchAction.异想的祥光, ActionType.OffGcd, ActionTargetType.Self),
        new("summon_seraph", "炽天召唤", SchAction.炽天召唤, ActionType.OffGcd, ActionTargetType.Self),
        new("consolation", "慰藉", SchAction.慰藉, ActionType.OffGcd, ActionTargetType.Self),
        new("expedient", "疾风怒涛之计", SchAction.疾风怒涛之计, ActionType.OffGcd, ActionTargetType.Self),
        new("indom", "不屈不挠之策", SchAction.不屈不挠之策, ActionType.OffGcd, ActionTargetType.Self),
        new("lustrate", "生命活性法", SchAction.生命活性法, ActionType.OffGcd, ActionTargetType.Self),
        new("sacred_soil", "野战治疗阵", SchAction.野战治疗阵, ActionType.OffGcd, ActionTargetType.Target),
        new("excogitation", "深谋远虑之策", SchAction.深谋远虑之策, ActionType.OffGcd, ActionTargetType.Self),
        new("seraphism", "炽天附体", SchAction.炽天附体, ActionType.OffGcd, ActionTargetType.Self),
    ];

    private string _skill = "chain";
    private string _target = TargetDefault;
    private string _mode = ModeQueue;
    private bool _highPriority = true;

    public string NodeDisplayName => "学者技能执行";

    public NodeParamInfo[] Params =>
    [
        new(
            FieldSkill,
            "技能",
            "选择要由时间轴执行的学者技能。",
            "select",
            Skills.Select(skill => (skill.Key, skill.Label)).ToArray()),
        new(
            FieldTarget,
            "目标",
            "默认目标会使用该技能的推荐目标；需要定点治疗时可改成指定队员。",
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
        var action = skill.Type == ActionType.Gcd
            ? HealerUtils.Gcd(skill.ActionId, target)
            : HealerUtils.OffGcd(skill.ActionId, target);

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

    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, TypeKey, dto =>
        {
            var action = new ScholarSkillAction();
            if (dto.Params == null) return action;

            if (dto.Params.TryGetValue(FieldSkill, out var skill))
                action._skill = skill;
            if (dto.Params.TryGetValue(FieldTarget, out var target))
                action._target = target;
            if (dto.Params.TryGetValue(FieldMode, out var mode))
                action._mode = mode;
            if (dto.Params.TryGetValue(FieldHighPriority, out var highPriority))
                action._highPriority = bool.TryParse(highPriority, out var parsedHighPriority) && parsedHighPriority;

            return action;
        });
    }
}
