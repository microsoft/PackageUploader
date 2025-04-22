using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class PackageCreationView : System.Windows.Controls.UserControl
{
    private readonly PackageCreationViewModel _viewModel;

    public PackageCreationView(PackageCreationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        this.Loaded += (s, e) => viewModel.OnAppearing();

        // Register drag drop event handlers after control is initialized
        this.Loaded += (s, e) => RegisterDragDropHandlers();
    }

    private void RegisterDragDropHandlers()
    {
        // Enable drag and drop for the TextBoxes
        DragDropHelper.RegisterTextBoxDragDrop(GamePathTextBox, _viewModel.GameDataPathDroppedCommand, true);
        DragDropHelper.RegisterTextBoxDragDrop(MappingDataTextBox, path => _viewModel.MappingDataXmlPath = path, false);
        DragDropHelper.RegisterTextBoxDragDrop(PackagePathTextBox, path => _viewModel.PackageFilePath = path, true);
        DragDropHelper.RegisterTextBoxDragDrop(SubValPathTextBox, path => _viewModel.SubValPath = path, true);
    }
}