using Dalamud.Game.ClientState.Objects.Types;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.DPS.Summoner.SMNData;

namespace MilkVio.DPS.Summoner;

public static class SummonerHelper
{
    public static bool IsBahamutAttuned()
    {
        if (SMNSkill.毁荡.GetAdjustedActionId() == SMNSkill.星极脉冲 || SMNSkill.毁荡.GetAdjustedActionId() == SMNSkill.灵泉之炎 ||
            SMNSkill.毁荡.GetAdjustedActionId() == SMNSkill.灵极脉冲)
        {
            return true;
        }

        return false;
    }

    public static bool IsTitanDone(IBattleChara me)
    {
        if (JobGaugeHelper.SMN.IsTitanAttuned) return false;
        return true;
    }

    public static bool IsGarudaDone(IBattleChara me)
    {
        if (JobGaugeHelper.SMN.IsGarudaAttuned) return false;
        if (me.HasStatus(SMNBuff.螺旋气流预备)) return false;
        return true;
    }

    public static bool IsIfritDone(IBattleChara me)
    {
        if (JobGaugeHelper.SMN.IsIfritAttuned) return false;
        if (me.HasStatus(SMNBuff.深红强袭预备)) return false;
        if (me.HasStatus(SMNBuff.深红旋风预备)) return false;
        return true;
    }

    public static 龙神类型 GetCurrent龙神类型()
    {
        var adjustActionId = SMNSkill.毁荡.GetAdjustedActionId();
        switch (adjustActionId)
        {
            case SMNSkill.星极脉冲:
                return 龙神类型.龙神;
            case SMNSkill.灵泉之炎:
                return 龙神类型.不死鸟;
            case SMNSkill.灵极脉冲:
                return 龙神类型.烈日龙神;
            default:
                return 龙神类型.None;
        }
    }

    public static PAction? AutoPetSummon()
    {
        if (JobGaugeHelper.SMN.HasPet) return null;
        return new PAction(SMNSkill.宝石兽召唤, ActionType.Gcd, ActionTargetType.Self);
    }

}
