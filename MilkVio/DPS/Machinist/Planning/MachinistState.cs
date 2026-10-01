using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Planning;

// 仅包含值；预测推进副本，绝不写回量谱、QT或宿主队列。
public record struct MachinistState
{
    public MachinistState() { }

    public long Now { get; init; }
    public ulong PlayerId { get; init; }
    public ulong TargetId { get; init; }
    public int Level { get; init; }
    public bool Alive { get; init; }
    public bool InCombat { get; init; }
    public bool HasTarget { get; init; }
    public int Targets { get; init; }
    public bool WildfireQt { get; init; }
    public bool HyperchargeQt { get; init; }
    public bool BarrelQt { get; init; }
    public bool FullMetalQt { get; init; } = true;
    public bool QueenQt { get; init; }
    public bool ReassembleQt { get; init; }
    public bool AoeQt { get; init; }
    public bool SawFirst { get; init; }
    public bool DumpQt { get; init; }
    public bool FastBurstQt { get; init; }
    public int Heat { get; set; }
    public int Battery { get; set; }
    public float Gcd { get; init; }
    public float GcdLeft { get; set; }
    public float GcdTotal { get; set; }
    public float Lock { get; set; }
    // 最近普通武器技能观察到的锁长，仅用于预测推进；插槽验算仍用WeaveLock。
    public float ActionLock { get; init; }
    public float WeaveLock { get; init; }
    public int Weaves { get; set; }
    public int MaxWeaves { get; init; }
    // 宿主快照可能正处于短G单插；它不代表后续普通GCD的上限。
    public int NormalMaxWeaves { get; init; }
    public uint Combo { get; set; }
    public float ComboLeft { get; set; }
    // 原生末段ID/计时可能尚未清空；只有仍可接第二、三段才有连击需要保护。
    // 保留原始字段供日志核对，求解与预测统一使用下一段的语义。
    public readonly uint ComboNext => float.IsFinite(ComboLeft) && ComboLeft > 0 ? Combo switch
    {
        MCHSkill.热分裂弹1 or MCHSkill.分裂弹1 => MCHSkill.热独头弹2,
        MCHSkill.热独头弹2 or MCHSkill.独头弹2 => MCHSkill.热狙击弹3,
        _ => 0
    } : 0;
    public float Drill { get; set; }
    public float DrillRecast { get; init; }
    public float AnchorCd { get; set; }
    public float AnchorRecast { get; init; }
    public float SawCd { get; set; }
    public float SawRecast { get; init; }
    public float WildfireCd { get; set; }
    public float BarrelCd { get; set; }
    public float HyperchargeCd { get; set; }
    // 最近一次实际超荷的短期恢复资格，不是新资源，也不延长过热Buff。
    public float HyperchargeRecoveryLeft { get; set; }
    public float ReassembleCharges { get; set; }
    public float Reassemble { get; set; }
    public float Gauss { get; set; }
    public float Ricochet { get; set; }
    public float FreeHypercharge { get; set; }
    public float Excavator { get; set; }
    public float FullMetal { get; set; }
    // 真实预备仍计时；关闭QT只解除自动消耗要求，不能伪造Buff消失或允许枪管覆盖。
    public readonly bool FullMetalPending => FullMetalQt && FullMetal > 0;
    public float Overheat { get; set; }
    public int OverheatStacks { get; set; }
    public float QueenLeft { get; set; }
    public float QueenCd { get; set; }
    public float PartyLeft { get; set; }
    // 仅供日志诊断的团辅观察值；资源决策统一使用自身野火/枪管周期。
    public float? PartyCycle { get; set; }
    public float PotionLeft { get; set; }
    public float WildfireLeft { get; set; }
    public int WildfireHits { get; set; }
    // 先野火路线只允许一个普通GCD，随后必须接超荷。
    public bool WildfireLead { get; set; }
    public bool LeadGcdDone { get; set; }
    // 本轮已应用的路线不随快速QT关闭而改变；低于100级不使用这条路线。
    public bool FastWildfire { get; set; }
    // 提前超荷/错过原插槽后的补挂，只兑现现有过热，不承诺再消耗一份超荷。
    public bool WildfireRecovery { get; set; }
    public bool WindowActive { get; init; }
    public int WindowVersion { get; init; }
    public float WindowLeft { get; set; }
    public float WindowLimit { get; set; }
    public int GoalHeat { get; init; }
    public int GoalBattery { get; init; }
    public bool ReserveEarly { get; init; }
    public float ReserveLeft { get; set; }

    public readonly bool Heated => Overheat > 0 && OverheatStacks > 0;
    public readonly bool FastBurst => Level == 100 && FastBurstQt;
    public readonly bool FastWildfireActive => Level == 100 && FastWildfire && WildfireLeft > 0;
    public readonly bool CanHypercharge => HyperchargeQt && !Heated && HyperchargeCd <= 0
        && Reassemble <= 0 && (FreeHypercharge > 0 || Heat >= 50);
    public readonly int WeaveLimit => GcdTotal < 1.8f ? 1 : Math.Clamp(MaxWeaves, 1, 2);
    // 团辅字段只供诊断；规划按自己的野火/枪管周期，不因队伍或目标切换改变留热。
    public readonly float DamageWindow => PotionLeft;
    public readonly bool IsDump => DumpQt || WindowActive && WindowLeft <= 20;
    public readonly int QtKey => (WildfireQt ? 1 : 0) | (HyperchargeQt ? 2 : 0) | (BarrelQt ? 4 : 0)
        | (QueenQt ? 8 : 0) | (ReassembleQt ? 16 : 0) | (AoeQt ? 32 : 0) | (SawFirst ? 64 : 0) | (DumpQt ? 128 : 0)
        | (FastBurst ? 256 : 0) | (FullMetalQt ? 512 : 0);
    public readonly uint HeatAction => AoeQt && Targets >= 3 ? MCHSkill.自动弩 : MCHSkill.烈焰弹;
}

internal readonly record struct MachinistChoice(uint Action, string Reason, float Delay = 0)
{
    // 窗口预测选出的整备目标随候选提交，不能重新用非窗口排序猜测。
    public uint Tool { get; init; }
    public bool RecoveryWildfire { get; init; }
}
