using PackageUploader.UI.ViewModel;
using System.Windows.Controls;

namespace PackageUploader.UI.View;

public partial class LoginView : System.Windows.Controls.UserControl
{
	public LoginView(LoginViewModel loginViewModel)
	{
		InitializeComponent();
		DataContext = loginViewModel;
	}
}