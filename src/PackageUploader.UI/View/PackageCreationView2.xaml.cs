using PackageUploader.UI.ViewModel;
using System.Windows.Controls;

namespace PackageUploader.UI.View;

public partial class PackageCreationView2 : System.Windows.Controls.UserControl
{
	public PackageCreationView2(PackageCreationViewModel viewModel)
	{
		InitializeComponent();
		DataContext = viewModel;
	}
}