// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Providers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.Json;

namespace PackageUploader.UI.ViewModel;

public partial class BaseViewModel : INotifyPropertyChanged
{
    private static CompactModeProvider? _compactModeProvider;
    private static readonly List<WeakReference<BaseViewModel>> _instances = [];

    /// <summary>
    /// Initializes compact mode binding for all ViewModels. Called once from App.xaml.cs at startup.
    /// </summary>
    public static void InitializeCompactMode(CompactModeProvider provider)
    {
        _compactModeProvider = provider;
        provider.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CompactModeProvider.IsCompactMode))
                NotifyAllInstances(nameof(IsCompactMode));
        };
    }

    private static void NotifyAllInstances(string propertyName)
    {
        lock (_instances)
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                if (_instances[i].TryGetTarget(out var vm))
                    vm.OnPropertyChanged(propertyName);
                else
                    _instances.RemoveAt(i);
            }
        }
    }

    public BaseViewModel()
    {
        lock (_instances)
        {
            _instances.Add(new WeakReference<BaseViewModel>(this));
        }
    }

    public bool IsCompactMode => _compactModeProvider?.IsCompactMode ?? false;

    protected static readonly string _settingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XboxPackageTool");
    
    private static readonly string _settingsFile = Path.Combine(_settingsFolder, "settings.json");

    // Shared lock for settings.json file access. Used by both BaseViewModel
    // and CompactModeProvider to prevent concurrent read-modify-write corruption.
    internal static readonly object SettingsFileLock = new();

    /// <summary>
    /// Writes a key/value pair into the shared in-memory settings cache and persists it.
    /// Use this instead of writing settings.json directly so the cache stays consistent.
    /// </summary>
    internal static void SetExternalSetting(string key, string value)
    {
        _settings[key] = value;
        SaveSettings();
    }

    /// <summary>
    /// Reads a value from the shared in-memory settings cache.
    /// </summary>
    internal static string GetExternalSetting(string key)
        => _settings.TryGetValue(key, out var value) ? value : string.Empty;

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
        lock (SettingsFileLock)
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
            catch
            {
                // Best-effort settings load — file may not exist or be malformed
            }

            return new Dictionary<string, string>();
        }
    }

    private static void SaveSettings()
    {
        lock (SettingsFileLock)
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
            catch
            {
                // Best-effort settings save
            }
        }
    }
}
