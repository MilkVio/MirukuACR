using MilkVio.DPS.Samurai.SAMData;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using PromeRotation.Managers;
using PromeRotation.Updaters;
using GameActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager;
using GameActionType = FFXIVClientStructs.FFXIV.Client.Game.ActionType;

namespace MilkVio.DPS.Samurai.Level100;

internal static class Samurai100Planning
{
    // 旧SDK没有此项，运行时按宿主设置读取。
    private static readonly System.Reflection.PropertyInfo? MaxWeavesSetting = typeof(HackSettings).GetProperty("MaxOgcdsPerGcd");
    public static int MaxWeaves => MaxWeavesSetting?.GetValue(PromeSettings.Instance.Hacks) is int value ? Math.Clamp(value, 1, 2) : 2;
    internal static Func<long> Clock = () => Environment.TickCount64;
    public static long Now => Clock();
    private static Samurai100State _state;
    private static Samurai100Forecast? _plan;
    private static long _readAt, _lastAttempt, _deadline;
    private static bool _entered, _pending, _immediate, _hadPotion;
    private static uint _effectSequence, _effectAction;
    private static long _effectAt;
    public static System.Action<string>? WriteNote;
    public static string Status { get; private set; } = "未安排择时用药";
    public static string Description
    {
        get
        {
            if (Core.Me == null || !Samurai100Helper.Enabled || !GameData.IsInCombat()) return "当前使用基础循环";
            var s = ReadBudgetState();
            return $"{Samurai100Rules.Phase(s)} 兑现余量={Samurai100Rules.SelfWindow(s):F1}秒 " +
                   $"剑气预留={Samurai100Rules.Reserve(s)} 下刀产气={Samurai100Rules.NextGain(s)} " +
                   $"药={s.Potion:F1} 团辅={s.Party:F1}；{_plan?.Summary ?? "基础循环自然兑现"}";
        }
    }
    public static bool PotionPending => _pending;

    public static void Enter()
    {
        Reset("进入SAM");
        _entered = true;
    }

    public static void Exit()
    {
        Reset("退出SAM");
        _entered = false;
        WriteNote = null;
    }

    public static void Reset(string reason)
    {
        if (_pending || _plan != null) WriteNote?.Invoke($"规划重置：{reason}");
        _pending = _immediate = _hadPotion = false;
        _lastAttempt = _deadline = _readAt = _effectAt = 0;
        _effectSequence = _effectAction = 0;
        _plan = null;
        Status = $"未安排择时用药（{reason}）";
    }

    public static void RequestPotion(float seconds)
    {
        if (!_entered || !Samurai100Helper.Enabled || Core.Me == null || Core.Me.IsDead || !GameData.IsInCombat())
        { Status = "择时用药仅在百级单体战斗接管中使用"; WriteNote?.Invoke(Status); return; }
        if (!float.IsFinite(seconds) || seconds < 0 || seconds > 30)
        { Status = "允许等待须在0至30秒之间"; WriteNote?.Invoke(Status); return; }
        if (_pending) { WriteNote?.Invoke("已有用药请求，保留原截止时间"); return; }
        if (Samurai100Burst.PotionLeft > 0) { Status = "已有药效，不重复吃药"; WriteNote?.Invoke(Status); return; }
        _immediate = seconds == 0;
        _deadline = Now + (long)(Math.Max(.25f, seconds) * 1000);
        _pending = true;
        _plan = null; _readAt = 0;
        Status = $"择时用药，允许等待{seconds:F1}秒";
        WriteNote?.Invoke(Status);
    }

    public static void Update(long now)
    {
        if (!_entered) return;
        if (Core.Me == null || Core.Me.IsDead || !GameData.IsInCombat() || !Samurai100Helper.Enabled)
        {
            if (_pending || _plan != null) Reset("死亡、脱战或离开百级单体");
            return;
        }
        var s = ReadState();
        if (s.Potion > 0 && !_hadPotion)
        {
            if (_pending || _lastAttempt > 0) FinishPotion("已观察到实际药效");
            WriteNote?.Invoke($"药内重新规划，剩余{s.Potion:F2}秒");
        }
        if (s.Potion <= 0 && _hadPotion)
            WriteNote?.Invoke("药效结束，按当前资源接回循环");
        _hadPotion = s.Potion > 0;
        if (_pending && now > _deadline) FinishPotion(_lastAttempt > 0 ? "用药期限结束，尚未观察到成功" : "用药期限结束，没有合法用药位置");
        if (Core.Target == null || Core.Target.IsDead || !Core.Target.IsTargetable || s.Target == s.Player || s.Casting)
        { _plan = null; return; }
        var preparing = Samurai100Rules.Preparing(s);
        // 提前一个明镜充能周期看下一次120，不保存轮次。
        var nextBurst = s.UseIki && s.IkiCd <= 55 && s.Ogi <= 0 && !s.OgiReturn && s.Zanshin <= 0;
        var window = Samurai100Rules.Spending(s) ? Samurai100Rules.SelfWindow(s) : 0;
        if (preparing) window = Math.Max(window, s.IkiCd + 30);
        if (nextBurst) window = Math.Max(window, s.IkiCd + 30);
        if (s.BattleTime >= 0 && s.BattleTime < 25) window = Math.Min(window, 25 - s.BattleTime);
        if (s.Potion > 0) window = s.Potion;
        else if (s.Party > 0) window = Math.Max(window, s.Party);
        // 平稳期也看续花和下一次明镜，避免到爆发才临时拼资源。
        if (!_pending && window <= 0) window = Math.Clamp(s.Dot + 5, 20, 40);
        if (now - _readAt < 80 && SameResources(s, _state)) return;
        _readAt = now;
        s.ReturnIsOld = s.ReturnLeft > 0;
        s.WindowStart = s.Potion > 0 || _pending || !preparing && !nextBurst ? 0 : Math.Max(0, s.IkiCd - s.Gcd);
        if (_pending)
        {
            var item = GameData.GetBestPotionId();
            s.PotionReadyAt = item == 0 ? float.PositiveInfinity : ActionHelper.GetItemCooldown(item);
        }
        _state = s;
        var wait = _pending ? Math.Max(0, (_deadline - now) / 1000f) : -1;
        _plan = Samurai100Projection.Find(s, Math.Min(85, window), wait);
    }

    private static void FinishPotion(string reason)
    {
        Status = reason;
        WriteNote?.Invoke(reason);
        _pending = false; _immediate = false; _plan = null;
    }

    public static void ObserveEffect(ulong source, uint action, uint sequence, long now)
    {
        if (!_entered || Core.Me == null || source != Core.Me.EntityId) return;
        if (_effectSequence == sequence && _effectAction == action && now - _effectAt < 1000) return;
        _effectSequence = sequence; _effectAction = action; _effectAt = now;
        _plan = null; _readAt = 0;
        var big = action == SAMSkill.纷乱雪月花 || action == SAMSkill.天道雪月花 || action == SAMSkill.回返雪月花 ||
                  action == SAMSkill.天道回返雪月花 || action == SAMSkill.奥义斩浪 || action == SAMSkill.回返斩浪;
        if (big) WriteNote?.Invoke($"爆发实际技能={SamuraiDebugLog.ActionName(action)} 药剩余={Samurai100Burst.PotionLeft:F2}");
    }

    public static void ObserveCancel(ulong source)
    {
        if (!_entered || Core.Me == null || source != Core.Me.EntityId) return;
        _plan = null; _readAt = 0;
        if (Samurai100Burst.PotionLeft > 0 || _pending)
            WriteNote?.Invoke("读条中断，按实际剩余药效和资源重算");
        else WriteNote?.Invoke("读条中断，按当前资源自然兑现，不补记已完成刀数");
    }

    public static bool TryGcd(out uint action, out string reason)
    {
        action = 0; reason = "";
        if (!ValidPlan()) return false;
        action = _plan!.Gcd; reason = _plan.Summary;
        // 预测会用的明镜、意气尚未成功时，不提前选出无效技能。
        var live = ReadState();
        if (action == SAMSkill.雪风 && live.MirrorStacks > 0 && !live.Dump)
            reason = "异常兜底：已有明镜仅缺雪，月花无法继续";
        if (action == SAMSkill.月光 && live.MirrorStacks <= 0 && live.Combo != SAMSkill.阵风) return false;
        if (action == SAMSkill.花车 && live.MirrorStacks <= 0 && live.Combo != SAMSkill.士风) return false;
        if (action == SAMSkill.奥义斩浪 && live.Ogi <= live.GcdLeft + live.OgiCast) return false;
        return true;
    }

    public static bool TryOff(uint action, out bool use, out string reason)
    {
        use = false; reason = "";
        if (!ValidPlan()) return false;
        use = _plan!.OffGcd == action;
        reason = use ? _plan.Summary : _plan.OffGcd == 0 ? "当前没有适合插入的技能" :
            $"当前优先{SamuraiDebugLog.ActionName(_plan.OffGcd)}；{Samurai100Rules.Phase(_state)}";
        if (action == SAMSkill.必杀剑_震天)
        {
            var spend = Samurai100Rules.SpendKenki(ReadBudgetState(), out var kenkiReason);
            if (use && spend || !use && !spend) reason = kenkiReason;
        }
        return true;
    }

    public static bool TryPrediction(uint nextAction, out SamuraiPrediction prediction)
    {
        prediction = default;
        if (!ValidPlan() || _plan!.CurrentOnly || _plan.Gcd != nextAction || _plan.Prediction.Action == 0) return false;
        prediction = _plan.Prediction;
        return true;
    }

    private static bool ValidPlan()
    {
        return _entered && _plan?.Computed == true && Samurai100Helper.Enabled && GameData.IsInCombat() && Core.Me != null &&
               !Core.Me.IsDead && Core.Target != null && Core.Target.IsTargetable && !Core.Target.IsDead &&
               Now - _readAt <= 250 && SameResources(ReadState(), _state);
    }

    private static bool SameResources(Samurai100State a, Samurai100State b)
    {
        return a.Player == b.Player && a.Target == b.Target && a.Sen == b.Sen && a.Kenki == b.Kenki && a.Meditation == b.Meditation &&
            a.Combo == b.Combo && a.MirrorStacks == b.MirrorStacks && (int)a.MirrorCharges == (int)b.MirrorCharges &&
            (a.ReturnLeft > 0) == (b.ReturnLeft > 0) && (a.Tendo > 0) == (b.Tendo > 0) && a.OgiReturn == b.OgiReturn &&
            (a.Ogi > 0) == (b.Ogi > 0) && (a.Zanshin > 0) == (b.Zanshin > 0) && (a.Potion > 0) == (b.Potion > 0) &&
            (a.Moon > 0) == (b.Moon > 0) && (a.Flower > 0) == (b.Flower > 0) && (a.Dot > 0) == (b.Dot > 0) &&
            (a.IkiCd <= 0) == (b.IkiCd <= 0) && (a.SeneiCd <= 0) == (b.SeneiCd <= 0) &&
            (a.ShohaCd <= 0) == (b.ShohaCd <= 0) && (a.ShintenCd <= 0) == (b.ShintenCd <= 0) &&
            (a.ZanshinCd <= 0) == (b.ZanshinCd <= 0) && a.MaxWeaves == b.MaxWeaves &&
            a.ReturnTendo == b.ReturnTendo && Math.Abs(a.Gcd - b.Gcd) < .001f &&
            (a.Distance <= a.MeleeRange) == (b.Distance <= b.MeleeRange) &&
            (a.Distance <= a.IaiRange) == (b.Distance <= b.IaiRange) &&
            (a.Distance <= a.OgiRange) == (b.Distance <= b.OgiRange) &&
            (a.Distance <= a.ShohaRange) == (b.Distance <= b.ShohaRange) &&
            a.Moving == b.Moving && a.Casting == b.Casting && Math.Abs(a.Distance - b.Distance) < .1f &&
            a.Position == b.Position && a.NeedsPosition == b.NeedsPosition &&
            (a.TrueNorth > a.GcdLeft + Samurai100Helper.EffectMargin) == (b.TrueNorth > b.GcdLeft + Samurai100Helper.EffectMargin) &&
            (a.TrueNorth > a.GcdLeft + a.Gcd + Samurai100Helper.EffectMargin) == (b.TrueNorth > b.GcdLeft + b.Gcd + Samurai100Helper.EffectMargin) &&
            Samurai100Rules.Phase(a) == Samurai100Rules.Phase(b) && (a.Eye > 0) == (b.Eye > 0) &&
            (a.Party > 0) == (b.Party > 0) &&
            a.UseDot == b.UseDot && a.UseMirror == b.UseMirror && a.UseIki == b.UseIki && a.UseSenei == b.UseSenei &&
            a.UseOgi == b.UseOgi && a.UseZanshin == b.UseZanshin && a.UseShinten == b.UseShinten && a.UseShoha == b.UseShoha &&
            a.Immediate == b.Immediate && a.Dump == b.Dump;
    }

    // 只从自动能力技入口调用；起手期间宿主不会走这里。
    public static unsafe bool TryUsePotionNow(long now)
    {
        try { return TryUsePotion(now); }
        catch (Exception ex)
        {
            FinishPotion($"用药异常，恢复基础循环：{ex.Message}");
            _lastAttempt = now;
            return true;
        }
    }

    private static unsafe bool TryUsePotion(long now)
    {
        if (_lastAttempt > 0 && now - _lastAttempt < 1400) return true;
        if (!_pending) return false;
        if (now > _deadline) { FinishPotion("用药期限结束"); return false; }
        if (!Samurai100Helper.Enabled || Core.Me == null || Core.Me.IsDead || !GameData.IsInCombat())
        { FinishPotion("已离开百级单体战斗"); return false; }
        if (Samurai100Burst.PotionLeft > 0) { FinishPotion("已有实际药效"); return false; }
        var item = GameData.GetBestPotionId();
        if (item == 0) { FinishPotion("未找到可用爆发药"); return false; }
        if (ActionHelper.GetItemCooldown(item) > Math.Max(0, (_deadline - now) / 1000f))
        { FinishPotion("药冷却无法赶上本次期限"); return false; }
        var manager = GameActionManager.Instance();
        var legal = manager != null && Core.Target != null && Core.Target.EntityId != Core.Me.EntityId && Core.Target.IsTargetable && !Core.Target.IsDead &&
                    !Core.Me.IsCasting && Samurai100Helper.GcdRemain >= Samurai100Projection.PotionLock &&
                    ActionHelper.GetAnimationLock() <= 0 && !ActionUpdater.HasActiveCommand() && !ActionQueueManager.HasActionsInQueue() &&
                    manager->GetActionStatus(GameActionType.Item, item, Core.Me.GameObjectId) == 0;
        if (!legal)
        {
            if (_immediate) FinishPotion("当前没有合法吃药插入位");
            return false;
        }
        var hasPlan = ValidPlan() && _plan!.PotionAt >= 0;
        var futureSlot = hasPlan && _plan!.PotionAt > .15f && _plan.PotionAt <= (_deadline - now) / 1000f;
        var lastSlot = !futureSlot && _deadline - now <= (Samurai100Helper.GcdSeconds + Samurai100Projection.PotionLock) * 1000;
        if (MoveManager.IsLocalPlayerMoving && !lastSlot && !_immediate) return false;
        if (!_immediate && !lastSlot && (!hasPlan || _plan!.PotionAt > .15f)) return false;
        _lastAttempt = now;
        Status = lastSlot ? "期限前按当前最佳安排尝试用药" : "已选定插入位，尝试用药";
        WriteNote?.Invoke($"{Status}；{_plan?.Summary}");
        // 即时调用宿主，避免药留在普通队列里过期后执行。
        ActionUpdater.UseAction(new PAction(item, ActionType.Item, ActionTargetType.Self));
        _plan = null; _readAt = 0;
        return true;
    }

    internal static Samurai100State ReadBudgetState()
    {
        var s = ReadState();
        if (!ValidPlan() || _plan!.CurrentOnly) return s;
        // 诊断也使用当前候选，避免日志预算和实际选择相反。
        s.Projecting = true; s.Order = _plan.Order; s.MirrorsLeft = _plan.Order / 4;
        s.MirrorTiming = _plan.MirrorTiming;
        s.FlowerDelay = _plan.FlowerDelay;
        return s;
    }

    internal static Samurai100State ReadState()
    {
        var me = Core.Me!;
        return new Samurai100State
        {
            Player = me.EntityId, Target = Core.Target?.EntityId ?? 0,
            Sen = (JobGaugeHelper.SAM.HasYuki ? 1 : 0) | (JobGaugeHelper.SAM.HasMoon ? 2 : 0) | (JobGaugeHelper.SAM.HasHana ? 4 : 0),
            Kenki = JobGaugeHelper.SAM.剑气, Meditation = JobGaugeHelper.SAM.剑压,
            MaxWeaves = MaxWeaves,
            Gcd = Samurai100Helper.GcdSeconds, GcdLeft = Samurai100Helper.GcdRemain,
            Cast = Samurai100Helper.CastSeconds(SAMSkill.彼岸花), OgiCast = Samurai100Helper.CastSeconds(SAMSkill.奥义斩浪),
            Combo = Samurai100Helper.GetComboId(), ComboLeft = ActionHelper.GetComboLeftTime(),
            Moon = me.GetStatusLeftTime(SAMBuff.风月), Flower = me.GetStatusLeftTime(SAMBuff.风花), Dot = Samurai100Helper.HiganbanaLeft,
            MirrorCharges = SamuraiHelper.明镜止水层数(),
            MirrorStacks = me.GetStatusLeftTime(SAMBuff.明镜止水) > 0 ? Math.Clamp(me.GetStatusStackCount(SAMBuff.明镜止水), 1, 3) : 0,
            MirrorLeft = me.GetStatusLeftTime(SAMBuff.明镜止水), Tendo = me.GetStatusLeftTime(SAMBuff.天道),
            ReturnLeft = SamuraiHelper.燕回返LeftTime(), ReturnTendo = SAMSkill.燕回返.GetAdjustedActionId() == SAMSkill.天道回返雪月花,
            Ogi = me.GetStatusLeftTime(SAMBuff.奥义浪斩预备), OgiReturn = SamuraiHelper.回返斩浪可用(), Zanshin = me.GetStatusLeftTime(SAMBuff.残心预备),
            IkiCd = SAMSkill.意气冲天.GetActionCooldown(), SeneiCd = SAMSkill.必杀剑_闪影.GetActionCooldown(),
            ShohaCd = SAMSkill.照破.GetActionCooldown(), ShintenCd = SAMSkill.必杀剑_震天.GetActionCooldown(), ZanshinCd = SAMSkill.残心.GetActionCooldown(),
            Potion = Samurai100Burst.PotionLeft, Party = Samurai100Burst.PartyBuffLeft,
            Distance = Core.Target == null ? 100 : me.DistanceToMe(), Moving = MoveManager.IsLocalPlayerMoving, Casting = me.IsCasting,
            MeleeRange = GameData.GetCurrentMeleeRange(), IaiRange = GameData.GetCurrentAttackRange(6),
            OgiRange = GameData.GetCurrentAttackRange(8), ShohaRange = GameData.GetCurrentAttackRange(10),
            BattleTime = (float)EngageManager.GetBattleTime(), Eye = me.GetStatusLeftTime(SAMBuff.天眼通),
            Position = TargetHelper.GetTargetPositional(), TrueNorth = me.GetStatusLeftTime(1250),
            NeedsPosition = Core.Target != null && TargetHelper.HasPositionalRequirement(Core.Target),
            UseDot = Samurai100Helper.UseHiganbana, UseMirror = PromeSettings.Instance.GetQt(SAMQt.明镜止水),
            UseIki = SamuraiHelper.AllowIkishoten, UseSenei = SamuraiHelper.AllowSenei, UseOgi = SamuraiHelper.AllowOgi,
            UseZanshin = SamuraiHelper.AllowZanshin, UseShinten = SamuraiHelper.AllowShinten, UseShoha = PromeSettings.Instance.GetQt(SAMQt.照破),
            Immediate = PromeSettings.Instance.GetQt(SAMQt.立即回返), Dump = PromeSettings.Instance.GetQt(SAMQt.倾泻资源)
        };
    }
}
