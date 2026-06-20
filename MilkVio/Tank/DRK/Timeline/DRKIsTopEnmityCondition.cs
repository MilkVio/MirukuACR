using System;
using System.Collections.Generic;
using PromeRotation.Timeline.Core;

namespace MilkVio.Tank.DRK.Timeline;

public sealed class DRKIsTopEnmityCondition
    : ICondition, IImmediateCondition, ISerializableCondition, IJobNodeDescriptor
{
    // ── 参数 ──────────────────────────────────────────────────────
    private bool _negate = false;

    // ── IJobNodeDescriptor ────────────────────────────────────────
    public string NodeDisplayName => "自身为目标一仇";

    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo(
            "negate",
            "取反",
            "勾选：自身不是目标一仇时条件成立",
            "bool")
    };

    public string GetParam(string fieldName) => fieldName switch
    {
        "negate" => _negate.ToString(),
        _        => ""
    };

    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "negate")
            _negate = bool.TryParse(value, out var v) && v;
    }

    // ── ICondition ────────────────────────────────────────────────
    public bool EvaluateImmediate()
    {
        var me     = Core.Me;
        var target = Core.Target;
        if (me == null || target == null) return false;

        var isTop = target.TargetObject?.EntityId == me.EntityId;
        return _negate ? !isTop : isTop;
    }

    public bool EvaluateWait() => EvaluateImmediate();

    // ── ISerializableCondition ────────────────────────────────────
    public ConditionDto ToDto() => new ConditionDto
    {
        Type   = "drkistopenmity",
        Params = new Dictionary<string, string>
        {
            ["negate"] = _negate.ToString()
        }
    };

    // ── 工厂注册
    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, "drkistopenmity", dto =>
        {
            var c = new DRKIsTopEnmityCondition();
            if (dto.Params?.TryGetValue("negate", out var v) == true)
                c._negate = bool.TryParse(v, out var b) && b;
            return c;
        });
    }
}
