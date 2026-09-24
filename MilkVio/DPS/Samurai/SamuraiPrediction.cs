using MilkVio.DPS.Samurai.Level100;
using MilkVio.DPS.Samurai.SAMData;
using PromeRotation.Data;

namespace MilkVio.DPS.Samurai;

internal record struct SamuraiPrediction
{
    public uint NextAction, Action, OtherAction;
    public int Gcds;
    public string? Waiting;

    private string Kind => Action == SAMSkill.月光 ? "背身" : Action == SAMSkill.花车 ? "侧身" : "读条";
    public string Hint => Gcds == 1 && Action != 0 ? $"下G{Kind}" : "";
    public string CastHint => Gcds == 1 && IsCast(Action) ? $"{SamuraiDebugLog.ActionName(Action)} 别动" : "";
    public string Text => Waiting ?? (Action == 0 ? "暂无预测" : Gcds == 1 ? Hint :
        $"{Gcds}G后{(OtherAction != 0 ? "背/侧" : Kind == "背身" ? "背" : Kind == "侧身" ? "侧" : Kind)}");

    public static bool IsCast(uint action) => action == SAMSkill.彼岸花 || action == SAMSkill.纷乱雪月花 ||
        action == SAMSkill.天道雪月花 || action == SAMSkill.奥义斩浪 ||
        action == SAMSkill.天下五剑 || action == SAMSkill.天道五剑;

    public static uint LogicalAction(Samurai100State s, uint action)
    {
        if (action == SAMSkill.居合术) return s.SenCount == 1 ? SAMSkill.彼岸花 :
            s.SenCount == 2 ? SAMSkill.天下五剑 : s.SenCount == 3 ? SAMSkill.纷乱雪月花 : 0;
        if (action == SAMSkill.天道雪月花) return SAMSkill.纷乱雪月花;
        if (action == SAMSkill.天道五剑) return SAMSkill.天下五剑;
        if (action == SAMSkill.回返雪月花 || action == SAMSkill.天道回返雪月花) return SAMSkill.燕回返;
        return action;
    }

    public static SamuraiPrediction Key(Samurai100State s, uint action, int gcds, uint other = 0, string? waiting = null)
    {
        action = LogicalAction(s, action);
        if (action == SAMSkill.月光 || action == SAMSkill.花车)
        {
            if (!s.NeedsPosition || s.TrueNorth > s.GcdLeft + Samurai100Helper.EffectMargin) return default;
        }
        else if (!IsCast(action)) return default;
        if (action == SAMSkill.纷乱雪月花 && s.Tendo > s.GcdLeft + s.Cast + Samurai100Helper.EffectMargin)
            action = SAMSkill.天道雪月花;
        if (action == SAMSkill.天下五剑 && s.Tendo > s.GcdLeft + s.Cast + Samurai100Helper.EffectMargin)
            action = SAMSkill.天道五剑;
        return new SamuraiPrediction { Action = action, OtherAction = gcds > 1 && !IsCast(action) ? other : 0,
            Gcds = gcds, Waiting = waiting };
    }

    public static uint OtherFinisher(Samurai100State s, uint action)
    {
        if (action != SAMSkill.阵风 && action != SAMSkill.士风 || s.SenCount != 1 || (s.Sen & 1) == 0 ||
            Samurai100Projection.KeepSen(s)) return 0;
        // 两路增益与闪都允许时才分图。
        s.Time = 0; s.Position = Positional.Rear;
        var rear = Samurai100Rules.ChooseMoonFlower(s);
        s.Position = Positional.Flank;
        var flank = Samurai100Rules.ChooseMoonFlower(s);
        if (rear != SAMSkill.月光 || flank != SAMSkill.花车) return 0;
        return action == SAMSkill.阵风 ? SAMSkill.花车 : SAMSkill.月光;
    }

    public static SamuraiPrediction Read(uint nextAction)
    {
        var s = Samurai100Planning.ReadState();
        var result = Key(s, nextAction, 1);
        // 群攻只读实际下一刀，不套单体前瞻。
        if (!Samurai100Helper.Enabled) { result.NextAction = nextAction; return result; }
        if (result.Action == 0 && nextAction == 0)
        {
            var waiting = Samurai100Helper.WaitingIaijutsu();
            result = Key(s, waiting, 1, waiting: Samurai100Rules.WaitReason(s));
        }
        if (result.Action == 0 && nextAction != 0)
        {
            if (!Samurai100Planning.TryPrediction(LogicalAction(s, nextAction), out result))
                result = Samurai100Projection.Preview(s, LogicalAction(s, nextAction));
        }
        result.NextAction = nextAction;
        return result;
    }
}

// 只记录提示去重，不记录循环轮次。
internal sealed class SamuraiPredictionHints
{
    private readonly object _gate = new();
    private uint _action, _sequence;
    private ulong _target;
    private long _at;
    private bool _shown, _ready;

    public void Observe(ulong source, ulong target, uint action, uint sequence, long now)
    {
        if (Core.Me == null || source != Core.Me.EntityId || !IsGcd(action)) return;
        lock (_gate)
        {
            if (_action == action && _sequence == sequence && (sequence != 0 || now - _at < 1000)) return;
            _action = action; _sequence = sequence; _target = target; _at = now;
            _shown = _ready = false;
        }
    }

    public void Reset()
    {
        lock (_gate) { _action = _sequence = 0; _target = 0; _at = 0; _shown = _ready = false; }
    }

    public string Take(SamuraiPrediction prediction, Samurai100State s, long now)
    {
        lock (_gate)
        {
            if (_action == 0 || _shown) return "";
            if (_target != s.Target || now - _at > s.Gcd * 1000 + 250)
            { _shown = true; return ""; }
            if (s.Casting) return "";
            if (!_ready)
            {
                _ready = EffectVisible(_action, s);
                if (!_ready) { if (now - _at > 400) _shown = true; return ""; }
            }
            if (s.GcdLeft <= .3f || prediction.CastHint.Length == 0) return "";
            // 下一刀必须确实读条；等站定时也要核对回返的先后。
            var next = SamuraiPrediction.LogicalAction(s, prediction.NextAction);
            if (next == 0 && prediction.Waiting != null)
            {
                s.Moving = false;
                next = Samurai100Projection.NextResourceGcd(s);
            }
            if (!SamuraiPrediction.IsCast(next) ||
                next != SamuraiPrediction.LogicalAction(s, prediction.Action)) return "";
            _shown = true;
            return prediction.CastHint;
        }
    }

    private static bool IsGcd(uint action) => action == SAMSkill.晓风 || action == SAMSkill.刃风 ||
        action == SAMSkill.阵风 || action == SAMSkill.士风 || action == SAMSkill.雪风 ||
        action == SAMSkill.月光 || action == SAMSkill.花车 || action == SAMSkill.燕飞 ||
        action == SAMSkill.风雅 || action == SAMSkill.风光 || action == SAMSkill.满月 || action == SAMSkill.樱花 ||
        SamuraiPrediction.IsCast(action) || action == SAMSkill.回返雪月花 ||
        action == SAMSkill.天道回返雪月花 || action == SAMSkill.回返五剑 ||
        action == SAMSkill.天道回返五剑 || action == SAMSkill.回返斩浪;

    private static bool EffectVisible(uint action, Samurai100State s)
    {
        if (action == SAMSkill.晓风 || action == SAMSkill.阵风 || action == SAMSkill.士风) return s.Combo == action;
        if (action == SAMSkill.月光) return (s.Sen & 2) != 0 && s.Combo == 0;
        if (action == SAMSkill.花车) return (s.Sen & 4) != 0 && s.Combo == 0;
        if (action == SAMSkill.雪风) return (s.Sen & 1) != 0 && s.Combo == 0;
        if (action == SAMSkill.满月) return (s.Sen & 2) != 0;
        if (action == SAMSkill.樱花) return (s.Sen & 4) != 0;
        if (action == SAMSkill.彼岸花) return s.SenCount == 0 && s.Dot > 55;
        if (action == SAMSkill.纷乱雪月花 || action == SAMSkill.天道雪月花 ||
            action == SAMSkill.天下五剑 || action == SAMSkill.天道五剑) return s.SenCount == 0 && s.ReturnLeft > 0;
        if (action == SAMSkill.回返雪月花 || action == SAMSkill.天道回返雪月花 ||
            action == SAMSkill.回返五剑 || action == SAMSkill.天道回返五剑) return s.ReturnLeft <= 0;
        if (action == SAMSkill.奥义斩浪) return s.OgiReturn;
        if (action == SAMSkill.回返斩浪) return !s.OgiReturn;
        return true;
    }
}
