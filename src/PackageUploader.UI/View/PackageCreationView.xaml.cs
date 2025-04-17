using PackageUploader.UI.ViewModel;
using System.Windows.Controls;

namespace PackageUploader.UI.View;

public partial class PackageCreationView : System.Windows.Controls.UserControl
{
	public PackageCreationView(PackageCreationViewModel viewModel)
	{
		InitializeComponent();
		DataContext = viewModel;

        this.Loaded += (s, e) => viewModel.OnAppearing();
    }
}