using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.TokenProvider;
using PackageUploader.UI.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PackageUploader.UI.ViewModel
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAccessTokenProvider _service;
        public ICommand LoginCommand { get; }

        private string _loginCommandText = "Login";
        public string LoginCommandText 
        { 
            get => _loginCommandText;
            set => SetProperty(ref _loginCommandText, value); 
        } 

        public LoginViewModel(IAccessTokenProvider service) 
        {
            _service = service;
            LoginCommand = new Command(Login);
        }

        public async void Login()
        {
            CancellationToken token = CancellationToken.None;
            var login = await _service.GetTokenAsync(token).ConfigureAwait(false);
            if (login != null)
            {
                _loginCommandText = "Done!";
            }
            else
            {
                _loginCommandText = "Failure!";
            }

            /*AuthenticationProvider authenticationProvider = new AuthenticationProvider();
            var authToken = await authenticationProvider.GetTokenAsync();*/

        }
    }
}
