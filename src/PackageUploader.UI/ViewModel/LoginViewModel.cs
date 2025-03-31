using Microsoft.Maui.Controls.PlatformConfiguration;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.UI.Providers;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PackageUploader.UI.ViewModel
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly UserLoggedInProvider _userLoggedInProvider;
        private readonly IAccessTokenProvider _service;
        private CancellationTokenSource _cancellationTokenSource = new();
        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }

        private string _loginCommandText = "Login";
        public string LoginCommandText 
        { 
            get => _loginCommandText;
            set => SetProperty(ref _loginCommandText, value); 
        }

        //private bool _isSignedIn;
        public bool IsSignedIn
        {
            get => _userLoggedInProvider.UserLoggedIn;
            set
            {
                if (_userLoggedInProvider.UserLoggedIn != value)
                {
                    _userLoggedInProvider.UserLoggedIn = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        private string _signInStatus = "Not Signed In";
        public string SignInStatus
        {
            get => _signInStatus;
            set => SetProperty(ref _signInStatus, value);
        }

        private bool _isSigningIn;
        public bool IsSigningIn
        {
            get => _isSigningIn;
            set => SetProperty(ref _isSigningIn, value);
        }

        public LoginViewModel(IAccessTokenProvider service, UserLoggedInProvider userLoggedInProvider) 
        {
            _userLoggedInProvider = userLoggedInProvider;
            _service = service;
            LoginCommand = new Command(LoginAsync);
            CancelCommand = new Command(CancelLogin);
        }

        public async void LoginAsync()
        {
            if (IsSignedIn)
            {
                // Remove the token from the cache. This makes the assumption that
                // we're using CachableInteractiveBrowserCredentialAccessToken which
                // is true for the current implementation, which is just proving out
                // the signin/signout. Likely we'll want to eventually remove this DeleteCache
                // method.
                CachableInteractiveBrowserCredentialAccessToken.DeleteCache();
                UpdateSignInStatus(false, string.Empty);
            }
            else
            {
                try
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    IsSigningIn = true;

                    var loginTask = Task.Run(async () =>
                    {
                        return await _service.GetTokenAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    });

                    while (!loginTask.IsCompleted)
                    {
                        _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                        await Task.Delay(500); // Check every 500ms
                    }

                    var login = await loginTask;

                    if (login != null)
                    {
                        UpdateSignInStatus(true, login.AccessToken);
                    }
                    else
                    {
                        UpdateSignInStatus(false, string.Empty);
                        SignInStatus = "Failure!";
                    }
                }
                catch (OperationCanceledException)
                {
                    UpdateSignInStatus(false, string.Empty);
                    SignInStatus = "Cancelled!";
                }
                catch (Exception)
                {
                    UpdateSignInStatus(false, string.Empty);
                    SignInStatus = "Error!";
                }
                finally
                {
                    IsSigningIn = false;
                }
            }
        }

        private void CancelLogin()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void UpdateSignInStatus(bool isSignedIn, string accessToken)
        {
            IsSignedIn = isSignedIn;
            
            if (isSignedIn)
            {
                // Try to extract user name from the token
                SignInStatus = "Signed In";
                LoginCommandText = "Sign Out";
                UserName = GetNameFromToken(accessToken) ?? "Name not available";
            }
            else
            {
                SignInStatus = "Not Signed In";
                UserName = string.Empty;
                LoginCommandText = "Login";
            }
        }

        private static string? GetNameFromToken(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();

            if (handler.ReadToken(accessToken) is JwtSecurityToken jsonToken)
            {
                var claims = jsonToken.Claims;

                return claims.FirstOrDefault(c => c.Type == "name")?.Value;
            }

            return null;
        }
    }
}
