using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Managers;
using PromeRotation.Timeline.Core;
using PromeRotation.Updaters;

namespace MilkVio.Healer.SGE.Timeline;

public sealed class SageSkillAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    private sealed record SkillDef(string Key, string Label, uint ActionId, ActionType Type, ActionTargetType DefaultTarget);

    private const string TypeKey = "miosge_skill";

    private const string FieldSkill = "skill";
    private const string FieldTarget = "target";
    private const string FieldMode = "mode";
    private const string FieldHighPriority = "high_priority";

    private const string TargetDefault = "Default";
    private const string ModeQueue = "queue";
    private const string ModeForce = "force";

    private static readonly SkillDef[] Skills =
    [
        new("eukrasia", "均衡", SgeAction.均衡, ActionType.Gcd, ActionTargetType.Self),
        new("dosis", "注药 / 均衡注药", SgeAction.注药, ActionType.Gcd, ActionTargetType.Target),
        new("phlegma", "发炎", SgeAction.发炎, ActionType.Gcd, ActionTargetType.Target),
        new("toxikon", "箭毒", SgeAction.箭毒, ActionType.Gcd, ActionTargetType.Target),
        new("dyskrasia", "失衡", SgeAction.失衡, ActionType.Gcd, ActionTargetType.Self),
        new("eukrasian_dyskrasia", "均衡失衡", SgeAction.均衡失衡, ActionType.Gcd, ActionTargetType.Self),
        new("pneuma", "魂灵风息", SgeAction.魂灵风息, ActionType.Gcd, ActionTargetType.Target),
        new("esuna", "康复", RoleAction.康复, ActionType.Gcd, ActionTargetType.Self),
        new("raise", "复苏", SgeAction.复苏, ActionType.Gcd, ActionTargetType.Self),
        new("lucid", "醒梦", RoleAction.醒梦, ActionType.OffGcd, ActionTargetType.Self),
        new("psyche", "心神风息", SgeAction.心神风息, ActionType.OffGcd, ActionTargetType.Target),
        new("kardia", "心关", SgeAction.心关, ActionType.OffGcd, ActionTargetType.Self),
        new("physis", "自生", SgeAction.自生, ActionType.OffGcd, ActionTargetType.Self),
        new("ixochole", "寄生橡汁", SgeAction.寄生橡汁, ActionType.OffGcd, ActionTargetType.Self),
        new("kerachole", "坚角清汁", SgeAction.坚角清汁, ActionType.OffGcd, ActionTargetType.Self),
        new("druochole", "寄生清汁", SgeAction.寄生清汁, ActionType.OffGcd, ActionTargetType.Self),
        new("taurochole", "白牛清汁", SgeAction.白牛清汁, ActionType.OffGcd, ActionTargetType.Self),
        new("haima", "输血", SgeAction.输血, ActionType.OffGcd, ActionTargetType.Self),
        new("rhizomata", "根素", SgeAction.根素, ActionType.OffGcd, ActionTargetType.Self),
        new("panhaima", "泛输血", SgeAction.泛输血, ActionType.OffGcd, ActionTargetType.Self),
        new("philosophia", "哲学", SgeAction.哲学, ActionType.OffGcd, ActionTargetType.Self),
    ];

    private string _skill = "psyche";
    private string _target = TargetDefault;
    private string _mode = ModeQueue;
    private bool _highPriority = true;

    public string NodeDisplayName => "贤者技能执行";

    public NodeParamInfo[] Params =>
    [
        new(
            FieldSkill,
            "技能",
            "选择要由时间轴执行的贤者技能。",
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
            var action = new SageSkillAction();
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
