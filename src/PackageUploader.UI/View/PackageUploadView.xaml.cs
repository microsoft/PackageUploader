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

        // First load only: OnAppearing re-processes the selected package and restores saved
        // preferences, and a repeat registration would add duplicate drag drop handlers.
        this.OnFirstLoad(() =>
        {
            viewModel.OnAppearing();
            RegisterDragDropHandlers();
        });
    }

    private void RegisterDragDropHandlers()
    {
        // Enable drag and drop for the TextBoxes
        DragDropHelper.RegisterTextBoxDragDrop(PackagePathTextBox, _viewModel.FileDroppedCommand, false);
    }
}