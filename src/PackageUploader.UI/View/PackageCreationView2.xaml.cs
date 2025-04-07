using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class PackageCreationView2 : ContentPage
{
	public PackageCreationView2(PackageCreationViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}