// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PackageUploader.UI.ViewModel;

public partial class BaseViewModel : INotifyPropertyChanged
{
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
        Microsoft.Maui.Storage.Preferences.Set(this.GetType().Name+"_"+propertyName, value?.ToString());
        OnPropertyChanged(propertyName);
        return true;
    }

    protected bool SetPropertyInApplicationPreferences(string value, [CallerMemberName] string? propertyName = null)
    {
        Microsoft.Maui.Storage.Preferences.Set(this.GetType().Name + "_" + propertyName, value);
        return true;
    }

    protected String GetPropertyFromApplicationPreferences(string fieldName)
    {
        return Microsoft.Maui.Storage.Preferences.Get(this.GetType().Name + "_" + fieldName, string.Empty);
    }
}
