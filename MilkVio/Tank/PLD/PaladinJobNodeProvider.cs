using System;
using System.Collections.Generic;
using MilkVio.Tank.PLD.Timeline;
using PromeRotation.Timeline.Core;

namespace MilkVio.Tank.PLD;

public sealed class PaladinJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        PLDAutoIronAction.Register(context);
        PLDIsTopEnmityCondition.Register(context);
    }

    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        => new[]
        {
            ("自身为目标一仇", "检测自身是否为当前目标的第一仇恨",
                (Func<ICondition>)(() => new PLDIsTopEnmityCondition()))
        };

    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => new[]
        {
            ("自动盾姿", "根据参数自动开启或关闭钢铁信念",
                (Func<IAction>)(() => new PLDAutoIronAction()))
        };
}
