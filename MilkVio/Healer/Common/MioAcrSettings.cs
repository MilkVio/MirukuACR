using System.Text.Json;

namespace MilkVio.Healer.Common;

public sealed class MioAcrSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XIVLauncherCN",
        "pluginConfigs",
        "PromeRotation",
        "ACR",
        "MioACR",
        "MioACR.settings.json");

    public int SageOpenerIndex { get; set; }

    public int AstOpenerIndex { get; set; } = 1;

    public int AstSlideMode { get; set; }

    public bool AstNeutralSectBeforePull { get; set; }

    public int SchShieldOpenerIndex { get; set; }

    public int SchResourceOpenerIndex { get; set; }

    public int SchSwiftOpenerIndex { get; set; }

    public int SchAetherflowReserve { get; set; }

    public bool SchIsH1 { get; set; }

    public static MioAcrSettings Instance { get; } = Load();

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(Instance, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Settings are non-critical; failing to save should not break the rotation.
        }
    }

    private static MioAcrSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new MioAcrSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<MioAcrSettings>(json) ?? new MioAcrSettings();
        }
        catch
        {
            return new MioAcrSettings();
        }
    }
}
