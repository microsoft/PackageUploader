
using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

/// <summary>
/// Interaction logic for PackagingFinishedPage.xaml
/// </summary>
public partial class PackagingFinishedView : System.Windows.Controls.UserControl
{
    public PackagingFinishedView(PackagingFinishedViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
