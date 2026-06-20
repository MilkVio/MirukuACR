using System;
using System.Collections.Generic;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Viper;

public sealed class ViperJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {

    }

    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        => Array.Empty<(string, string, Func<ICondition>)>();

    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => Array.Empty<(string, string, Func<IAction>)>();
}
