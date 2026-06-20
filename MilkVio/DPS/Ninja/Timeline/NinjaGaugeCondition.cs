using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Ninja.Timeline;

public sealed class NinjaGaugeCondition
    : ICondition, ISerializableCondition, IJobNodeDescriptor
{
    // ── 参数 ──────────────────────────────────────────────────────
    private int _ninkiThreshold = 10;

    // ── IJobNodeDescriptor ────────────────────────────────────────
    public string NodeDisplayName => "忍气阈值检测";

    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo(
            "ninki_threshold",
            "忍气阈值",
            "忍气阈值低于此值时条件成立",
            "int")
    };

    public string GetParam(string fieldName) => fieldName switch
    {
        "ninki_threshold" => _ninkiThreshold.ToString(),
        _                 => ""
    };

    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "ninki_threshold" && int.TryParse(value, out var v))
            _ninkiThreshold = Math.Max(0, v);
    }

    // ── ICondition ────────────────────────────────────────────────
    public bool EvaluateImmediate()
        => JobGaugeHelper.NIN.Ninki <= _ninkiThreshold;

    public bool EvaluateWait() => EvaluateImmediate();

    // ── ISerializableCondition ────────────────────────────────────
    public ConditionDto ToDto() => new ConditionDto
    {
        Type   = "ninjagauge",
        Params = new Dictionary<string, string>
        {
            ["ninki_threshold"] = _ninkiThreshold.ToString()
        }
    };

    // 工厂注册
    public static void Register(RotationNodeContext context)
    {
        ConditionFactory.Register(context, "ninjagauge", dto =>
        {
            var c = new NinjaGaugeCondition();
            if (dto.Params?.TryGetValue("ninki_threshold", out var raw) == true
                && int.TryParse(raw, out var n))
                c._ninkiThreshold = n;
            return c;
        });
    }
}
