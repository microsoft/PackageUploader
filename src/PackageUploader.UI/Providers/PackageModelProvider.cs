// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PackageUploader.UI.Providers;

public partial class PackageModelProvider : INotifyPropertyChanged
{
    private PackageModel _package;

    public PackageModel Package
    {
        get => _package;
        set
        {
            if(value == null) //suggested by copilot
            {
                _package = new PackageModel();
                OnPropertyChanged();
            }
            else if (_package != value)
            {
                _package = value;
                OnPropertyChanged();
            }
        }
    }

    public string PackagingLogFilepath { get; internal set; } = string.Empty;

    public PackageModelProvider()
    {
        _package = new PackageModel();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
