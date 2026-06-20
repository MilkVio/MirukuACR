using PromeRotation.Data;
using MilkVio.Healer.WHM.WHMData;

namespace MilkVio.Healer.WHM;

public static class WhiteMageHelper
{
    public static uint GetCurrent闪灼()
    {
        var me = Core.Me;
        if (me == null) return 0;
        var level = me.Level;
        if (level >= 82)
        {
            return WHMSkill.闪灼;
        }
        else if(level >= 72)
        {
            return WHMSkill.闪耀;
        }
        else
        {
            return WHMSkill.崩石;
        }
    }
    
    public static uint GetCurrent毒ActionId()
    {
        var me = Core.Me;
        if (me == null) return 0;
        var level = me.Level;
        if (level >= 72)
        {
            return WHMSkill.天辉;
        }
        return WHMSkill.烈风;
    }
    
    public static uint GetCurrent毒StatusId()
    {
        var me = Core.Me;
        if (me == null) return 0;
        var level = me.Level;
        if (level >= 72)
        {
            return WHMBuff.天辉;
        }
        return WHMBuff.烈风;
    }
}
