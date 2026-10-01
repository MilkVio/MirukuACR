using MilkVio.DPS.Machinist.MCHData;

namespace MilkVio.DPS.Machinist.Planning;

// 用于候选路线比较的百级期望威力，不是装备面板DPS或全局最优证明。
// 7.5职业指南；必暴直相对普通命中的系数及未来团辅增益采用统一估值。
internal static class MachinistDamage
{
    internal const float BurstBonus = .10f;
    private const float CriticalDirect = 1.65f;

    internal static float Action(MachinistState s, uint action)
    {
        var potency = action switch
        {
            MCHSkill.热分裂弹1 => 220f,
            MCHSkill.热独头弹2 => s.ComboNext == MCHSkill.热独头弹2 ? 320 : 140,
            MCHSkill.热狙击弹3 => s.ComboNext == MCHSkill.热狙击弹3 ? 420 : 160,
            MCHSkill.钻头 or MCHSkill.空气锚 or MCHSkill.回转飞锯 or MCHSkill.掘地飞轮 => 660,
            MCHSkill.全金属爆发 => 900 * CriticalDirect,
            MCHSkill.烈焰弹 => 260, // 240 + 过热20
            MCHSkill.热冲击 => 220,
            MCHSkill.自动弩 or MCHSkill.双将 or MCHSkill.将死 => 180,
            MCHSkill.霰弹枪 => 130,
            _ => 0
        };
        if (s.Reassemble > 0 && MachinistRules.IsWeaponskill(action) && action != MCHSkill.全金属爆发)
            potency *= CriticalDirect;
        var extra = s.AoeQt ? Math.Max(0, s.Targets - 1) : 0;
        var falloff = action switch
        {
            MCHSkill.回转飞锯 or MCHSkill.掘地飞轮 or MCHSkill.全金属爆发 => .75f,
            MCHSkill.双将 or MCHSkill.将死 => .7f,
            MCHSkill.自动弩 or MCHSkill.霰弹枪 => 1,
            _ => 0
        };
        return potency * (1 + extra * falloff);
    }

    internal static float Multiplier(MachinistState s, float at)
    {
        if (at < s.DamageWindow) return 1 + BurstBonus;
        // 统一按自身120估计覆盖，不用队伍团辅强弱决定是否准备双过热。
        // 这个加成只比较召唤/输出窗口候选，不是平峰能否花掉预留热量的许可。
        var cd = MachinistRules.BurstAt(s);
        if (!float.IsFinite(cd)) return 1;
        var start = cd > 100 ? cd - 120 : cd;
        if (at >= start && (at - start) % 120 < 20) return 1 + BurstBonus;
        return 1;
    }
}
