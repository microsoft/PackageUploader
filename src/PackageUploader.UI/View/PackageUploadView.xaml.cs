// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class PackageUploadView : System.Windows.Controls.UserControl
{
    private readonly PackageUploadViewModel _viewModel;

    public PackageUploadView(PackageUploadViewModel viewModel)
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
        DragDropHelper.RegisterTextBoxDragDrop(PackagePathTextBox, _viewModel.FileDroppedCommand, false);
    }
}