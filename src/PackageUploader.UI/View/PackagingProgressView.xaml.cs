using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class PackagingProgressView : System.Windows.Controls.UserControl
{
	public PackagingProgressView(PackagingProgressViewModel viewModel)
	{
		InitializeComponent();
        DataContext = viewModel;
    }
}