// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PackageUploader.UI.Services;

public partial class PackageModelService : INotifyPropertyChanged
{
    private PackageModel _package;

    public PackageModel Package
    {
        get => _package;
        set
        {
            _package = value;
            OnPropertyChanged();
        }
    }

    public PackageModelService()
    {
        _package = new PackageModel();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
