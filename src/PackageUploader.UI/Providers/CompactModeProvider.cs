// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace PackageUploader.UI.Providers;

public class CompactModeProvider : INotifyPropertyChanged
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XboxPackageTool", "settings.json");

    private const string SettingsKey = "CompactModeProvider_IsCompactMode";
    private static readonly object _settingsLock = new();

    private bool _isCompactMode;

    public CompactModeProvider()
    {
        _isCompactMode = LoadSetting();
    }

    public bool IsCompactMode
    {
        get => _isCompactMode;
        set
        {
            if (_isCompactMode != value)
            {
                _isCompactMode = value;
                SaveSetting(value);
                OnPropertyChanged();
            }
        }
    }

    private bool LoadSetting()
    {
        lock (_settingsLock)
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null && dict.TryGetValue(SettingsKey, out var value))
                    {
                        return bool.TryParse(value, out var result) && result;
                    }
                }
            }
            catch
            {
                // Settings file may not exist or be malformed
            }
            return false;
        }
    }

    private void SaveSetting(bool value)
    {
        lock (_settingsLock)
        {
            try
            {
                var dict = new Dictionary<string, string>();
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? dict;
                }
                dict[SettingsKey] = value.ToString();
                var dir = Path.GetDirectoryName(SettingsFilePath);
                if (dir != null) Directory.CreateDirectory(dir);
                File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(dict));
            }
            catch
            {
                // Best-effort persistence
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
