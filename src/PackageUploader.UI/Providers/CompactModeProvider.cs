// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PackageUploader.UI.Providers;

public class CompactModeProvider : INotifyPropertyChanged
{
    private const string SettingsKey = "CompactModeProvider_IsCompactMode";

    private bool _isCompactMode;

    public CompactModeProvider()
    {
        // Read initial state from the shared settings cache (loaded once at startup by BaseViewModel).
        var stored = ViewModel.BaseViewModel.GetExternalSetting(SettingsKey);
        _isCompactMode = bool.TryParse(stored, out var result) && result;
    }

    public bool IsCompactMode
    {
        get => _isCompactMode;
        set
        {
            if (_isCompactMode != value)
            {
                _isCompactMode = value;
                // Route through the shared in-memory cache so BaseViewModel.SaveSettings()
                // never overwrites this value with a stale copy.
                ViewModel.BaseViewModel.SetExternalSetting(SettingsKey, value.ToString());
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
