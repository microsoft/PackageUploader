

using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

/// <summary>
/// Interaction logic for UploadingFinishedView.xaml
/// </summary>
public partial class UploadingFinishedView : System.Windows.Controls.UserControl
{
    public UploadingFinishedView(UploadingFinishedViewModel uploadingFinishedViewModel)
    {
        InitializeComponent();
        DataContext = uploadingFinishedViewModel;
    }
}
