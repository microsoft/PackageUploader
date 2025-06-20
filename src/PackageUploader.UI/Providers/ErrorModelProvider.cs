
using System.ComponentModel;
using PackageUploader.UI.Model;
using System.Runtime.CompilerServices;

namespace PackageUploader.UI.Providers;

public partial class ErrorModelProvider : INotifyPropertyChanged
{

    private ErrorModel _errorModel;
    public ErrorModel Error
    {
        get => _errorModel;
        set
        {
            if (value == null) //Idea suggested by copilot, not a bad idea
            {
                _errorModel = new ErrorModel();
                OnPropertyChanged();
            }
            else if (_errorModel != value)
            {
                _errorModel = value;
                OnPropertyChanged();
            }
        }
    }

    public ErrorModelProvider()
    {
        _errorModel = new ErrorModel();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
