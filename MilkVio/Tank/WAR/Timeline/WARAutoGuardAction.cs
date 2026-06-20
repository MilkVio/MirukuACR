using System.Collections.Generic;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Managers;
using MilkVio.Tank.WAR.WARData;
using PromeRotation.Timeline.Core;
using PromeRotation.Updaters;

namespace MilkVio.Tank.WAR.Timeline;

public sealed class WARAutoGuardAction
    : IAction, ISerializableAction, IJobNodeDescriptor
{
    // ── 参数 ──────────────────────────────────────────────────────
    private bool _enable = true;
    private bool _forceUse = false;

    // ── IJobNodeDescriptor ────────────────────────────────────────
    public string NodeDisplayName => "自动守护";

    public NodeParamInfo[] Params => new[]
    {
        new NodeParamInfo(
            "enable",
            "开启守护",
            "勾选 = 开启守护，不勾选 = 关闭守护",
            "bool"),
        new NodeParamInfo(
            "force_use",
            "强制使用",
            "勾选后不走队列强制释放",
            "bool")
    };

    public string GetParam(string fieldName) => fieldName switch
    {
        "enable"    => _enable.ToString(),
        "force_use" => _forceUse.ToString(),
        _           => ""
    };

    public void SetParam(string fieldName, string value)
    {
        switch (fieldName)
        {
            case "enable":
                _enable = bool.TryParse(value, out var v1) && v1;
                break;
            case "force_use":
                _forceUse = bool.TryParse(value, out var v2) && v2;
                break;
        }
    }
    // ── IAction ───────────────────────────────────────────────────
    public void Execute()
    {
        var player = Core.Me;

        if (_enable)
        {
            if (!player.HasStatus(WARBuff.守护))
            {
                var action = new PAction(WARSkill.守护, ActionType.OffGcd, ActionTargetType.Self);
                if (_forceUse)
                    ActionUpdater.UseAction(action);
                else
                    ActionQueueManager.Enqueue(action);
            }
        }
        else
        {
            if (player.HasStatus(WARBuff.守护))
            {
                var action = new PAction(WARSkill.守护, ActionType.OffGcd, ActionTargetType.Self);
                if (_forceUse)
                    ActionUpdater.UseAction(action);
                else
                    ActionQueueManager.Enqueue(action);
            }
        }
    }

    // ── ISerializableAction ───────────────────────────────────────
    public ActionDto ToDto() => new ActionDto
    {
        Type   = "warautoguard",
        Params = new Dictionary<string, string>
        {
            ["enable"]    = _enable.ToString(),
            ["force_use"] = _forceUse.ToString()
        }
    };

    // ── 工厂注册
    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, "warautoguard", dto =>
        {
            var a = new WARAutoGuardAction();
            if (dto.Params?.TryGetValue("enable", out var v) == true)
                a._enable = bool.TryParse(v, out var b) && b;
            if (dto.Params?.TryGetValue("force_use", out var f) == true)
                a._forceUse = bool.TryParse(f, out var fb) && fb;
            return a;
        });
    }
}
