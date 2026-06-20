using System.Collections.Generic;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Ninja.Timeline;

public sealed class NinjaForceNinjutsuAction
    : IAction, ISerializableAction, IJobNodeDescriptor
{
    // ── 参数 ──────────────────────────────────────────────────────
    private string _ninjutsuType = "Fuma";

    // ── IJobNodeDescriptor ────────────────────────────────────────
    public string NodeDisplayName => "强制忍术";

    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo(
            "ninjutsu_type",
            "忍术类型",
            "选择要强制施放的忍术",
            "enum",
            Options: new[]
            {
                ("Fuma",    "风魔手里剑"),
                ("Raiton",  "雷遁之术"),
                ("Hyoton",  "冰晶之术"),
                ("Katon",   "火遁之术"),
                ("Huton",   "风遁之术"),
                ("Doton",   "土遁之术"),
                ("Suiton",  "水遁之术"),
            })
    };

    public string GetParam(string fieldName) => fieldName switch
    {
        "ninjutsu_type" => _ninjutsuType,
        _               => ""
    };

    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "ninjutsu_type")
            _ninjutsuType = value;
    }

    // ── IAction ───────────────────────────────────────────────────
    public void Execute()
    {
        return;
    }

    // ── ISerializableAction ───────────────────────────────────────
    public ActionDto ToDto() => new ActionDto
    {
        Type   = "ninjaforceninjutsu",
        Params = new Dictionary<string, string>
        {
            ["ninjutsu_type"] = _ninjutsuType
        }
    };

    // ── 工厂注册
    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, "ninjaforceninjutsu", dto =>
        {
            var a = new NinjaForceNinjutsuAction();
            if (dto.Params?.TryGetValue("ninjutsu_type", out var t) == true)
                a._ninjutsuType = t;
            return a;
        });
    }
}
