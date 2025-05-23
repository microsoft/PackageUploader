using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PackageUploader.UI.Providers
{
    public partial class UserLoggedInProvider : INotifyPropertyChanged
    {
        private bool _isLoggedIn;
        public bool UserLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                if (_isLoggedIn != value)
                {
                    _isLoggedIn = value;
                    OnPropertyChanged(nameof(UserLoggedIn));
                }
            }
        }

        private string _accessToken = string.Empty;
        public string AccessToken
        {
            get => _accessToken;
            set
            {
                if (_accessToken != value)
                {
                    _accessToken = value;
                    OnPropertyChanged(nameof(AccessToken));
                }
            }
        }

        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged(nameof(UserName));
                }
            }
        }

        public UserLoggedInProvider()
        {
            UserLoggedIn = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
