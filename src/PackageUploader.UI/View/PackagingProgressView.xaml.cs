using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class PackagingProgressView : ContentPage
{
	public PackagingProgressView(PackageCreationViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}