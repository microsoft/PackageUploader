// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.Json;

namespace PackageUploader.UI.ViewModel;

public partial class BaseViewModel : INotifyPropertyChanged
{
    protected static readonly string _settingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XboxPackageTool");
    
    private static readonly string _settingsFile = Path.Combine(_settingsFolder, "settings.json");
    private static readonly Dictionary<string, string> _settings = LoadSettings();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        
        // Store the setting if propertyName is not null
        if (propertyName != null)
        {
            SetPropertyInApplicationPreferences(propertyName, value?.ToString() ?? string.Empty);
        }
        
        OnPropertyChanged(propertyName);
        return true;
    }

    protected bool SetPropertyInApplicationPreferences(string propertyName, string value)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return false;
        }
        
        string key = $"{GetType().Name}_{propertyName}";
        _settings[key] = value;
        SaveSettings();
        return true;
    }

    protected string GetPropertyFromApplicationPreferences(string fieldName)
    {
        string key = $"{GetType().Name}_{fieldName}";
        return _settings.TryGetValue(key, out string? value) ? value : string.Empty;
    }

    private static Dictionary<string, string> LoadSettings()
    {
        try
        {
            if (!Directory.Exists(_settingsFolder))
            {
                Directory.CreateDirectory(_settingsFolder);
            }

            if (File.Exists(_settingsFile))
            {
                string json = File.ReadAllText(_settingsFile);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
        }
        catch (Exception ex)
        {
            // Log exception or handle it appropriately
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }

        return new Dictionary<string, string>();
    }

    private static void SaveSettings()
    {
        try
        {
            if (!Directory.Exists(_settingsFolder))
            {
                Directory.CreateDirectory(_settingsFolder);
            }

            string json = JsonSerializer.Serialize(_settings);
            File.WriteAllText(_settingsFile, json);
        }
        catch (Exception ex)
        {
            // Log exception or handle it appropriately
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
}
