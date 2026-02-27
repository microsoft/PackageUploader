// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PackageUploader.UI.Providers;

public class StubPackageModeProvider : INotifyPropertyChanged
{
    private bool _isStubPackageMode = false;
    public bool IsStubPackageMode
    {
        get => _isStubPackageMode;
        set
        {
            if (_isStubPackageMode != value)
            {
                _isStubPackageMode = value;
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
