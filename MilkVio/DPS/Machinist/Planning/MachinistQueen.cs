namespace MilkVio.DPS.Machinist.Planning;

// 百级威力取职业指南；攻击时点是保守近似，不能把技能说明的12秒等同于召唤至退场。
// 907ae6日志中实际在场约20.1～20.5秒。将攻击和退场分开，原生在场状态仍优先。
internal static class MachinistQueen
{
    internal const float Duration = 20.5f;
    internal const float FirstHit = 5;
    internal const float FullPotency = 1570; // 50电：突进、五拳、打桩、冲击。
    private static readonly (float At, float Potency)[] Hits =
        [(5, 240), (8, 120), (9.5f, 120), (11, 120), (12.5f, 120), (14, 120), (17, 340), (19.5f, 390)];

    internal static float Coverage(float summonAt, float end, float start = 0)
    {
        var potency = 0f;
        foreach (var hit in Hits)
            if (summonAt + hit.At + MachinistRules.EffectMargin >= start && summonAt + hit.At + MachinistRules.EffectMargin <= end)
                potency += hit.Potency;
        return potency / FullPotency;
    }

    internal static float Damage(MachinistState initial, int battery, float summonAt, float end)
    {
        var damage = 0f;
        foreach (var hit in Hits)
        {
            var at = summonAt + hit.At;
            if (at + MachinistRules.EffectMargin <= end)
                damage += hit.Potency * battery / 50f * MachinistDamage.Multiplier(initial, at + MachinistRules.EffectMargin);
        }
        return damage;
    }
}
