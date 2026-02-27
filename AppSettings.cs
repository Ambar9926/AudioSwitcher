using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace AudioSwitcher;

public class AppSettings
{
    public string DeviceA { get; set; } = "";
    public string DeviceB { get; set; } = "";
    public uint HotkeyModifiers { get; set; } = HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT;
    public uint HotkeyKey { get; set; } = (uint)Keys.S;
    public bool StartWithWindows { get; set; } = false;

    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AudioSwitcher");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new();
            }
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            string json = JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}