using System;
using System.Collections.Generic;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Reaper;

public sealed class ReaperJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {

    }

    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        => Array.Empty<(string, string, Func<ICondition>)>();

    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => Array.Empty<(string, string, Func<IAction>)>();
}
