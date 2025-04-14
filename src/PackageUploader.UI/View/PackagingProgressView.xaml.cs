using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class PackagingProgressView : System.Windows.Controls.UserControl
{
	public PackagingProgressView(PackageCreationViewModel viewModel)
	{
		InitializeComponent();
        DataContext = viewModel;
    }
}