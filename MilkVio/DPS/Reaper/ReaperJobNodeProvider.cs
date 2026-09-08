using System;
using System.Collections.Generic;
using PromeRotation.Timeline.Core;
using MilkVio.DPS.Reaper.Timeline;
namespace MilkVio.DPS.Reaper;

public sealed class ReaperJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        ReaperOutputWindowAction.Register(context);
        ReaperClearWindowAction.Register(context);
        ReaperFastCircleAction.Register(context);
    }

    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        => Array.Empty<(string, string, Func<ICondition>)>();

    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => [("设置可输出窗口", "预计结束与期末红绿；不改变QT", () => new ReaperOutputWindowAction()),
            ("清除可输出窗口", "解除窗口及期末资源要求", () => new ReaperClearWindowAction()),
            ("快速神秘环", "勾选启用，取消勾选恢复；仍受120 QT控制", () => new ReaperFastCircleAction())];
}
