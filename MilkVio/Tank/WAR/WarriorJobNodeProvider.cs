using System;
using System.Collections.Generic;
using MilkVio.Tank.WAR.Timeline;
using PromeRotation.Timeline.Core;

namespace MilkVio.Tank.WAR;

public sealed class WarriorJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        WARAutoGuardAction.Register(context);
        WARIsTopEnmityCondition.Register(context);
    }

    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        => new[]
        {
            ("自身为目标一仇", "检测自身是否为当前目标的第一仇恨",
                (Func<ICondition>)(() => new WARIsTopEnmityCondition()))
        };

    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => new[]
        {
            ("自动守护", "根据参数自动开启或关闭守护",
                (Func<IAction>)(() => new WARAutoGuardAction()))
        };
}
