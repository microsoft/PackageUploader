using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class PackageUploadingView : System.Windows.Controls.UserControl
{
    public PackageUploadingView(PackageUploadingViewModel packageUploadingViewModel)
    {
        InitializeComponent();
        DataContext = packageUploadingViewModel;
    }
}
