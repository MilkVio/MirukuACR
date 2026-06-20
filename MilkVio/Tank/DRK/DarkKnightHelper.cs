using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using PromeRotation.Data;
using PromeRotation.Extensions;
using PromeRotation.Helpers;
using MilkVio.Tank.DRK.DRKData;

namespace MilkVio.Tank.DRK;

public static class DarkKnightHelper
{
    public static void RestQT()
    {
        PromeSettings.Instance.SetQt(DRKQt.启用起手, true);
        PromeSettings.Instance.SetQt(DRKQt.不打60, false);
        PromeSettings.Instance.SetQt(DRKQt.不打120, false);
        PromeSettings.Instance.SetQt(DRKQt.保留3000蓝, true);
        PromeSettings.Instance.SetQt(DRKQt.倾泻资源, false);
        PromeSettings.Instance.SetQt(DRKQt.延后掠影的蔑视, true);
        PromeSettings.Instance.SetQt(DRKQt.马桶对齐120, false);
        PromeSettings.Instance.SetQt(DRKQt.伤残, true);
        PromeSettings.Instance.SetQt(DRKQt.最终爆发, false);
    }

    public static int GetEdgeofShadowCount(bool includeDarkArt)
    {
        if (Core.Me == null) return 0;
        
        var hasDarkArt = JobGaugeHelper.DRK.HasDarkArts;
        var darkArt = 0;
        
        if (hasDarkArt && includeDarkArt)
        {
            darkArt = 1;
        }

        return (int)Core.Me.CurrentMp / 3000 + darkArt;
    }

    public static PAction? AutoMalice(bool mode)
    {
        var player = Core.Me;
        if (player == null) return null;
        
        if (mode)
        {
            if (!player.HasStatus(DRKBuff.深恶痛绝))
            {
                return new PAction(DRKSkill.深恶痛绝, ActionType.OffGcd, ActionTargetType.Self);
            }

            return null;
        }

        if (mode == false)
        {
            if (player.HasStatus(DRKBuff.深恶痛绝))
            {
                return new PAction(DRKSkill.解除深恶痛绝, ActionType.OffGcd, ActionTargetType.Self);;
            }
        }

        return null;
    }
}
