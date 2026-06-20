using Bard.Data;

namespace Bard;
public static class BardHelper
{
    public static uint GetNormalShootActionCurrentId()
    {
        var me = PromeRotation.Core.Core.Me;
        if (me.Level < 76) return BardSkill.强力射击;
        return BardSkill.爆发射击;
    }
    
    public static uint GetStraightShootActionCurrentId()
    {
        var me = PromeRotation.Core.Core.Me;
        if (me.Level < 70) return BardSkill.直线射击;
        return BardSkill.辉煌箭;
    }
}
