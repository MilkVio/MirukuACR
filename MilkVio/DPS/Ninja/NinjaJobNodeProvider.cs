using System;
using System.Collections.Generic;
using MilkVio.DPS.Ninja.Timeline;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Ninja;

public sealed class NinjaJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        NinjaGaugeCondition.Register(context);
        NinjaForceNinjutsuAction.Register(context);
    }

    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        => new[]
        {
            ("忍气阈值检测", "忍气阈值低于此值时条件成立",
                (Func<ICondition>)(() => new NinjaGaugeCondition()))
        };

    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => new[]
        {
            ("强制忍术", "强制施放选定的忍术类型",
                (Func<IAction>)(() => new NinjaForceNinjutsuAction()))
        };
}
