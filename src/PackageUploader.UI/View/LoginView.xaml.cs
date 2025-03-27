using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class LoginView : ContentPage
{
	public LoginView(LoginViewModel loginViewModel)
	{
		InitializeComponent();
		BindingContext = loginViewModel;
	}
}