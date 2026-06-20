using System;
using System.Collections.Generic;
using MilkVio.Tank.DRK.Timeline;
using PromeRotation.Timeline.Core;

namespace MilkVio.Tank.DRK;

public sealed class DarkKnightJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        DRKAutoMaliceAction.Register(context);
        DRKIsTopEnmityCondition.Register(context);
    }

    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        => new[]
        {
            ("自身为目标一仇", "检测自身是否为当前目标的第一仇恨",
                (Func<ICondition>)(() => new DRKIsTopEnmityCondition()))
        };

    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => new[]
        {
            ("自动深恶痛绝", "根据参数自动开启或关闭深恶痛绝",
                (Func<IAction>)(() => new DRKAutoMaliceAction()))
        };
}
