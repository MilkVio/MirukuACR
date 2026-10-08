using PromeRotation.Timeline.Core;

namespace MilkVio.DPS.Samurai.Timeline;

public sealed class SamuraiClearAction(bool meditation = false) : IAction, ISerializableAction, IJobNodeDescriptor
{
    private string TypeKey => meditation ? "milkviosamclearmeditation" : "milkviosamclearmeikyo";
    public string NodeDisplayName => meditation ? "清理默想状态" : "清除当前明镜请求";
    public NodeParamInfo[] Params => [];
    public string GetParam(string fieldName) => "";
    public void SetParam(string fieldName, string value) { }
    public void Execute()
    {
        if (meditation) SamuraiMeditation.Clear("时间轴主动清理");
        else SamuraiTimeline.ClearRequest("时间轴主动清除");
    }
    public ActionDto ToDto() => new() { Type = TypeKey };
    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, "milkviosamclearmeikyo", _ => new SamuraiClearAction());
        ActionFactory.Register(context, "milkviosamclearmeditation", _ => new SamuraiClearAction(true));
    }
}
