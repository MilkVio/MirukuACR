using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Planning;

// ACR拥有的短期提交记录。交给旧宿主后不能撤销；本地超时也不返还本G插槽。
// 实战和队列模拟共用此类，不读取宿主的私有计数或执行器内部字段。
internal sealed class MachinistDispatch
{
    private const int StatusGraceMs = 350;
    private sealed class Receipt(uint action, uint adjusted, MachinistState before, long cycle)
    {
        public readonly uint Action = action, Adjusted = adjusted;
        public readonly MachinistState Before = before;
        public readonly long Cycle = cycle;
        public bool EffectSeen, Applied;
        public uint Sequence;
    }

    private readonly List<Receipt> _receipts = new();
    private readonly Queue<(uint Action, uint Sequence)> _seen = new();
    private Receipt? _pending;
    private long _cycle, _pairCycle, _pairUntil, _reserveUntil, _hyperchargeAt;
    private long _wildfireReadyAt;
    private ulong _pairTarget;
    private int _pairWindow;
    private long _recastStarted;
    private uint _recastAction;
    private float _recastElapsed, _recastTotal;
    private bool _recastActive;
    private int _used;
    private uint _reservedTool;
    private bool _sawReassemble, _solving;
    public System.Action<string>? WriteNote;
    public int Weaves => _used;
    public bool Pending => _pending != null;
    public uint ReservedTool => _reservedTool;

    public void Reset()
    {
        _receipts.Clear(); _seen.Clear(); _pending = null;
        _cycle = _pairCycle = _pairUntil = _reserveUntil = _hyperchargeAt = 0;
        _wildfireReadyAt = 0; _pairTarget = 0; _pairWindow = 0;
        _recastStarted = 0; _recastAction = 0; _recastElapsed = _recastTotal = 0; _recastActive = false;
        _used = 0; _reservedTool = 0; _sawReassemble = _solving = false;
    }

    // 同一帧内求解耗时、暂停或重复读取时，墙钟会变化，原生Elapsed未必推进。
    // 用复唱从无到有、Elapsed回绕或动作/周期变化识别新G，不能不断重算now-Elapsed。
    public long RecastStarted(long now, bool active, uint action, float total, float elapsed)
    {
        active &= float.IsFinite(total) && float.IsFinite(elapsed) && total > 0 && elapsed >= 0;
        if (active && (!_recastActive || action != _recastAction || Math.Abs(total - _recastTotal) > .1f
            || elapsed < _recastElapsed - .05f))
            _recastStarted = now - (long)(elapsed * 1000);
        _recastActive = active; _recastAction = action; _recastTotal = total; _recastElapsed = elapsed;
        return active ? _recastStarted : 0;
    }

    // started来自真实复唱起点。进入输入窗口/读到GcdLeft=0均不能提前重置预算。
    public void ObserveGcd(long now, long started)
    {
        if (started <= 0 || _cycle != 0 && Math.Abs(started - _cycle) < 100) return;
        // 中途启用时不知道这一G已插了几个，保守等下一次真实GCD；GCD求解照常运行。
        _used = _cycle == 0 && now - started > StatusGraceMs ? 2 : 0;
        _cycle = started;
        _pairCycle = _pairUntil = 0;
    }

    public void Update(MachinistState s, long started, bool hostBusy)
    {
        ObserveGcd(s.Now, started);
        if (s.WildfireCd > .001f) _wildfireReadyAt = s.Now + (long)(s.WildfireCd * 1000);
        if (!s.WildfireQt || !s.HyperchargeQt || !s.Alive || !s.HasTarget
            || s.TargetId != _pairTarget || s.WindowVersion != _pairWindow) _pairUntil = 0;
        _receipts.RemoveAll(r => r != _pending && s.Now - r.Before.Now > 10000);
        if (_pending is { } pending)
        {
            if (!pending.Applied && Applied(pending, s))
            {
                pending.Applied = true;
                // 极端排队跨G后才生效，费用也必须记在实际这轮，不能沿用上一G的预留。
                if (Math.Abs(pending.Cycle - _cycle) >= 100) _used++;
            }
            if (!hostBusy && (pending.Applied || s.Now - pending.Before.Now >= StatusGraceMs))
            {
                WriteNote?.Invoke(pending.Applied
                    ? $"提交已观察到效果或状态变化：{pending.Action}，本G已占用={_used}"
                    : $"宿主已空闲但未观察到成功：{pending.Action}；结束本地等待，本G插槽仍保留");
                _pending = null;
                if (pending.Action == MCHSkill.整备)
                    _reserveUntil = pending.Applied ? s.Now + StatusGraceMs : 0;
                if (!pending.Applied && pending.Action == MCHSkill.超荷) _pairUntil = 0;
            }
        }
        if (_reservedTool != 0)
        {
            if (s.Reassemble > 0) _sawReassemble = true;
            else if (_pending?.Action != MCHSkill.整备 && (_sawReassemble || s.Now >= _reserveUntil))
            { _reservedTool = 0; _sawReassemble = false; }
        }
    }

    // 只接收自身能力技效果；AOE展开按序列去重，起手和手动能力技也计入当前G。
    public void ObserveAbility(uint action, uint sequence, long at)
    {
        if (_seen.Contains((action, sequence))) return;
        _seen.Enqueue((action, sequence)); if (_seen.Count > 64) _seen.Dequeue();
        var receipt = _receipts.FirstOrDefault(r => !r.EffectSeen && at >= r.Before.Now
            && (r.Action == action || r.Adjusted == action));
        if (receipt != null)
        {
            receipt.EffectSeen = true;
            receipt.Sequence = sequence;
            if (!receipt.Applied && Math.Abs(receipt.Cycle - _cycle) >= 100) _used++;
            receipt.Applied = true;
        }
        else if (_cycle > 0) _used++;
        if (action == MCHSkill.超荷) _hyperchargeAt = at;
        if (action == MCHSkill.野火) _pairUntil = 0;
    }

    // 使用提交时的路线，而非效果到达时的QT；固定起手/手动野火没有快速提交记录。
    public bool WasFastWildfire(uint sequence, ulong target) => _receipts.Any(r => r.Action == MCHSkill.野火
        && r.EffectSeen && r.Sequence == sequence && r.Before.TargetId == target && r.Before.FastBurst);

    public uint HeldTool(MachinistState s) => s.Reassemble <= 0 && _reservedTool != 0
        && (_pending?.Action == MCHSkill.整备 || s.Now < _reserveUntil) ? _reservedTool : 0;

    public MachinistChoice NextGcd(MachinistState s)
    {
        _solving = true;
        if (_hyperchargeAt > 0 && s.Now - _hyperchargeAt < StatusGraceMs && !s.Heated)
            return new(0, "超荷效果已到，等待短暂状态同步");
        return MachinistProjection.SelectGcd(s, _reservedTool, HeldTool(s));
    }

    public MachinistChoice NextOffGcd(MachinistState s, MachinistBudget budget, bool hostBusy, uint legacyTool = 0)
    {
        s.Weaves = _used;
        if (!s.Alive || !s.HasTarget) return new(0, "自身/目标不可用");
        if (hostBusy || _pending != null) return new(0, "等待已交给宿主的动作处理");
        if (!_solving)
        {
            // 起手队列刚清空时，最后一发能力技的效果可能还在途。不猜剩余插槽。
            _solving = true; _used = Math.Max(_used, s.WeaveLimit);
            return new(0, "首次接管当前GCD，下一次真实GCD重置插槽；GCD继续求解");
        }
        if (s.Lock > 0 || _cycle <= 0 || MachinistRules.Slots(s) == 0)
            return new(0, "本G没有安全插入位置");
        if (s.WildfireQt && s.HyperchargeQt && _pairUntil > s.Now && _pairCycle == _cycle
            && s.TargetId == _pairTarget && s.WindowVersion == _pairWindow && budget.WildfireHeat && !s.Heated)
            return new(0, "为野火保留第二插槽，等待超荷状态");
        if (HeldTool(s) != 0) return new(0, "等待整备状态同步");
        if (s.WildfireLeft <= 0 && _receipts.Any(r => r.Action == MCHSkill.野火 && r.Before.FastBurst
            && r.Before.TargetId == s.TargetId && r.Cycle == _cycle && !r.EffectSeen && r.Applied))
            return new(0, "快速野火已提交，保留本G后续插槽等待效果同步");

        var choice = s.Level == 100 ? MachinistProjection.SelectOff(s, budget)
            : s.Level >= 10 && s.ReassembleQt && s.ReassembleCharges >= 1 && s.Reassemble <= 0
                && !s.Heated && MachinistRules.IsTool(legacyTool) && s.GcdLeft < 4
                ? new MachinistChoice(MCHSkill.整备, "整备绑定下一工具") : new(0, "无整备安排");
        if (choice.Action == 0) return choice;
        // 延迟仅属于尚未提交的计划，不提前入队占住宿主活动命令。
        if (choice.Delay > s.GcdTotal - s.GcdLeft + .001f)
            return new(0, "等待野火路线的安全时点，到时重新求解");
        if (s.GcdLeft <= MachinistWildfire.SubmitTail(s))
            return new(0, "已过安全提交期限");
        return choice with { Delay = 0 };
    }

    public bool Issue(MachinistState s, uint action, uint adjusted, uint tool = 0)
    {
        s.Weaves = _used;
        if (_pending != null || MachinistRules.Slots(s) == 0 || action == 0) return false;
        _pending = new(action, adjusted, s, _cycle);
        _receipts.Add(_pending); _used++;
        if (tool != 0)
        {
            _reservedTool = tool; _sawReassemble = false;
            _reserveUntil = s.Now + StatusGraceMs;
        }
        if (action is MCHSkill.超荷 or MCHSkill.野火 && MachinistWildfire.TryPlan(s, out var plan, out _))
        {
            if (plan.Route == MachinistWildfireRoute.Fast)
            {
                var preview = MachinistFastBurst.Preview(s);
                WriteNote?.Invoke($"快速野火安排：目标={s.TargetId:X} 预计开火={plan.FireIn:F3}s后 " +
                    $"预计{preview.Hits}/6击 热={s.Heat} 免费={s.FreeHypercharge:F2} 超荷QT={s.HyperchargeQt} " +
                    "不足六击不阻止开火，后续按实况重算");
            }
            else WriteNote?.Invoke($"野火安排：{plan.Name} 冷却={s.WildfireCd:F3}s 预计开火={plan.FireIn:F3}s后 " +
                    $"第六击余量={10 - (plan.SixthIn - plan.FireIn):F3}s 工具推迟合计={plan.ToolDelay:F3}s " +
                    $"过热后首G={plan.Recovery}@{plan.RecoveryIn:F3}s 飞轮/全金属预备={s.Excavator:F2}/{s.FullMetal:F2}s");
            if (action == MCHSkill.超荷 && plan.Route == MachinistWildfireRoute.Pair)
            {
                _pairCycle = _cycle; _pairTarget = s.TargetId; _pairWindow = s.WindowVersion;
                _pairUntil = s.Now + (long)(Math.Max(0, s.GcdLeft - MachinistWildfire.SubmitTail(s)) * 1000);
            }
        }
        if (action == MCHSkill.野火)
            WriteNote?.Invoke(_wildfireReadyAt > 0
                ? $"野火提交：距预计冷却转好{Math.Max(0, s.Now - _wildfireReadyAt) / 1000f:F3}s"
                : "野火提交：未观察到此前冷却，无法计算本轮漂移");
        WriteNote?.Invoke($"交出单体能力技：{action} 工具={tool} 本G已占用={_used}/{s.WeaveLimit}");
        return true;
    }

    private static bool Applied(Receipt r, MachinistState s) => r.EffectSeen || r.Action switch
    {
        MCHSkill.整备 => s.Reassemble > 0 || s.ReassembleCharges < r.Before.ReassembleCharges - .5f,
        MCHSkill.超荷 => s.Heated || s.HyperchargeCd > r.Before.HyperchargeCd + 1,
        MCHSkill.野火 => s.WildfireCd > r.Before.WildfireCd + 1,
        MCHSkill.枪管加热 => s.BarrelCd > r.Before.BarrelCd + 1,
        MCHSkill.后式自走人偶 or MCHSkill.车式浮空炮塔 => s.QueenLeft > 0 || s.Battery < r.Before.Battery,
        MCHSkill.双将 or MCHSkill.虹吸弹 => s.Gauss < r.Before.Gauss - .5f,
        MCHSkill.将死 or MCHSkill.弹射 => s.Ricochet < r.Before.Ricochet - .5f,
        _ => false
    };
}
