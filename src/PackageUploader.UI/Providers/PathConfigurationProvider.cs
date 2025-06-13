// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PackageUploader.UI.Providers;

public partial class PathConfigurationProvider : INotifyPropertyChanged
{
    private string _makePkgPath = string.Empty;
    public string MakePkgPath
    {
        get => _makePkgPath;
        set
        {
            if (_makePkgPath != value)
            {
                _makePkgPath = value;
                OnPropertyChanged();
            }
        }
    }

    private string _packageUploaderPath = string.Empty;
    public string PackageUploaderPath
    {
        get => _packageUploaderPath;
        set
        {
            if (_packageUploaderPath != value)
            {
                _packageUploaderPath = value;
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
