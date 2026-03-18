// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class Msixvc2UploadView : System.Windows.Controls.UserControl
{
    private readonly Msixvc2UploadViewModel _viewModel;

    public Msixvc2UploadView(Msixvc2UploadViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        // Register drag drop event handlers after control is initialized
        this.Loaded += (s, e) => RegisterDragDropHandlers();
    }

    private void RegisterDragDropHandlers()
    {
        DragDropHelper.RegisterTextBoxDragDrop(ContentPathTextBox, _viewModel.ContentPathDroppedCommand, true);
        DragDropHelper.RegisterTextBoxDragDrop(MappingDataTextBox, path => _viewModel.MappingDataXmlPath = path, false);
    }
}
