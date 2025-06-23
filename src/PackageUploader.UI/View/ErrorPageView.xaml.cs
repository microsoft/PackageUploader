using PackageUploader.UI.ViewModel;


namespace PackageUploader.UI.View;

/// <summary>
/// Interaction logic for ErrorPageView.xaml
/// </summary>
public partial class ErrorPageView : System.Windows.Controls.UserControl
{
    public ErrorPageView(ErrorScreenViewModel errorScreenViewModel)
    {
        InitializeComponent();
        DataContext = errorScreenViewModel;
    }
}
