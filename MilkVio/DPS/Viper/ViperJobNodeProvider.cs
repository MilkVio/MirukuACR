using System;
using System.Collections.Generic;
using MilkVio.DPS.Viper.Timeline;
using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Viper;

public sealed class ViperJobNodeProvider : IJobNodeProvider
{
    public void RegisterNodes(RotationNodeContext context)
    {
        ViperGaugeCondition.Register(context);
        ViperPossessionCondition.Register(context);
    }

    public IReadOnlyList<(string, string, Func<ICondition>)> GetConditionDescriptors()
        =>
        [
            ("灵力值检测", "比较当前灵力值与设定值", () => new ViperGaugeCondition()),
            ("附体状态检测", "检测当前是否处于附体状态", () => new ViperPossessionCondition()),
        ];

    public IReadOnlyList<(string, string, Func<IAction>)> GetActionDescriptors()
        => Array.Empty<(string, string, Func<IAction>)>();
}
