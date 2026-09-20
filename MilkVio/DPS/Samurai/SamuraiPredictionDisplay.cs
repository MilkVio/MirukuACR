using System.Numerics;
using Dalamud.Bindings.ImGui;
using PromeRotation.Helpers;
using MilkVio.DPS.Samurai.SAMData;

namespace MilkVio.DPS.Samurai;

internal static class SamuraiPredictionDisplay
{
    public static void Draw(SamuraiPrediction prediction)
    {
        var settings = SAMSettings.Instance;
        if (!settings.显示技能预测) return;
        var width = float.IsFinite(settings.预测图标大小) ? Math.Clamp(settings.预测图标大小, 48, 144) : 72;
        var size = new Vector2(width, width * 4 / 3);
        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings |
            ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;
        if (settings.锁定预测窗口) flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs;
        var screen = ImGui.GetIO().DisplaySize;
        ImGui.SetNextWindowPos(new Vector2(screen.X * .5f + 80, screen.Y * .6f),
            settings.重置预测位置 ? ImGuiCond.Always : ImGuiCond.Once);
        settings.重置预测位置 = false;
        ImGui.SetNextWindowSize(size);
        ImGui.SetNextWindowBgAlpha(.55f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        try
        {
            var visible = ImGui.Begin("武士预测###MilkVioSamuraiPrediction", flags);
            try
            {
                if (!visible) return;
                var min = ImGui.GetWindowPos();
                var draw = ImGui.GetWindowDrawList();
                var max = min + new Vector2(width);
                if (prediction.OtherAction == 0) DrawIcon(draw, prediction.Action, min, max, 0, 1);
                else
                {
                    DrawIcon(draw, prediction.Action, min, max, 0, .5f);
                    DrawIcon(draw, prediction.OtherAction, min, max, .5f, 1);
                }
                var textMin = min + new Vector2(0, width);
                var textMax = min + size;
                var text = prediction.Text;
                var measured = ImGui.CalcTextSize(text);
                var scale = Math.Min(width / 72, Math.Min((width - 4) / Math.Max(1, measured.X),
                    (size.Y - width - 2) / Math.Max(1, measured.Y)));
                var position = (textMin + textMax - measured * scale) / 2;
                var fontSize = ImGui.GetFontSize() * scale;
                draw.AddText(ImGui.GetFont(), fontSize, position + Vector2.One, 0xE0000000, text);
                draw.AddText(ImGui.GetFont(), fontSize, position, 0xFFFFFFFF, text);
                ImGui.Dummy(size);
            }
            finally { ImGui.End(); }
        }
        finally { ImGui.PopStyleVar(); }
    }

    private static void DrawIcon(ImDrawListPtr draw, uint action, Vector2 min, Vector2 max, float left, float right)
    {
        var texture = action == 0 ? 66313u.GetGameIcon() : action.GetActionIcon();
        texture ??= 66313u.GetGameIcon();
        if (texture == null) return;
        var width = max.X - min.X;
        draw.AddImage(texture.Handle, new Vector2(min.X + width * left, min.Y),
            new Vector2(min.X + width * right, max.Y), new Vector2(left, 0), new Vector2(right, 1), 0xE6FFFFFF);
    }
}
