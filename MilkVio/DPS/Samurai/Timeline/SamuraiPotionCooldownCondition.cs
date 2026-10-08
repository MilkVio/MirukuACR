using System.Globalization;
using ECommons.ExcelServices;
using MilkVio.DPS.Samurai.Level100;
using PromeRotation.Data;
using PromeRotation.Helpers;
using PromeRotation.Timeline.Core;
using GameActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;

namespace MilkVio.DPS.Samurai.Timeline;

public sealed class SamuraiPotionCooldownCondition : ICondition, IImmediateCondition, ISerializableCondition, IJobNodeDescriptor
{
    private const string TypeKey = "milkviosampotioncooldown";
    private string _comparison = "lte", _seconds = "0", _readFailure = "";
    public string NodeDisplayName => "爆发药冷却";
    public NodeParamInfo[] Params =>
    [
        new("comparison", "比较符", "", "enum",
            [("gt", ">"), ("gte", ">="), ("eq", "="), ("ne", "!="), ("lte", "<="), ("lt", "<")]),
        new("seconds", "秒数", "无药时不满足；<=0为冷却转好", "float")
    ];
    public string GetParam(string fieldName) => fieldName switch
    { "comparison" => _comparison, "seconds" => _seconds, _ => "" };
    public void SetParam(string fieldName, string value)
    {
        if (fieldName == "comparison") _comparison = value;
        if (fieldName == "seconds") _seconds = value;
    }
    public unsafe bool EvaluateImmediate()
    {
        if (!float.TryParse(_seconds, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) ||
            !float.IsFinite(seconds) || seconds < 0) return false;
        if (Core.Me == null || Core.Me.ClassJob.RowId != (uint)Job.SAM) return false;
        try
        {
            if (GameActionManager.Instance() == null) return Unavailable("冷却接口不可读");
            var item = GameData.GetBestPotionId();
            if (item == 0) return Unavailable("未携带可用爆发药");
            var cd = ActionHelper.GetItemCooldown(item);
            if (!float.IsFinite(cd) || cd < 0) return Unavailable("冷却数据无效");
            _readFailure = "";
            return _comparison switch
            {
                "gt" => cd > seconds, "gte" => cd >= seconds,
                "eq" => Math.Abs(cd - seconds) < .05f, "ne" => Math.Abs(cd - seconds) >= .05f,
                "lte" => cd <= seconds, "lt" => cd < seconds, _ => false
            };
        }
        catch (Exception ex) { return Unavailable(ex.Message); }
    }
    private bool Unavailable(string reason)
    {
        if (_readFailure != reason) Samurai100Planning.WriteNote?.Invoke($"爆发药冷却检测：{reason}");
        _readFailure = reason;
        return false;
    }
    public bool EvaluateWait() => EvaluateImmediate();
    public ConditionDto ToDto() => new() { Type = TypeKey, Params = new() { ["comparison"] = _comparison, ["seconds"] = _seconds } };
    public static void Register(RotationNodeContext context) => ConditionFactory.Register(context, TypeKey, dto =>
    {
        var condition = new SamuraiPotionCooldownCondition();
        if (dto.Params != null) foreach (var pair in dto.Params) condition.SetParam(pair.Key, pair.Value);
        return condition;
    });
}
