// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.ViewModel;
using System.IO;
using System.Windows.Controls;

namespace PackageUploader.UI.View;

public partial class PackageUploadView : System.Windows.Controls.UserControl
{
    private readonly PackageUploadViewModel _viewModel;

    public PackageUploadView(PackageUploadViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        
        // Subscribe to the Loaded event to refresh UI when the control is loaded
        this.Loaded += (s, e) => _viewModel.OnAppearing();

        // Register drag drop event handlers after control is initialized
        this.Loaded += (s, e) => RegisterDragDropHandlers();
    }

    private void RegisterDragDropHandlers()
    {
        // Enable drag and drop for the TextBoxes
        RegisterTextBoxDragDrop(PackagePathTextBox, _viewModel.FileDroppedCommand, false);
    }

    private void RegisterTextBoxDragDrop(System.Windows.Controls.TextBox textBox, Action<string> onDropAction, bool acceptFolders)
    {
        if (textBox == null) return;

        textBox.AllowDrop = true;

        textBox.PreviewDragOver += (sender, e) =>
        {
            e.Effects = System.Windows.DragDropEffects.None;

            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }

            e.Handled = true;
        };

        textBox.Drop += (sender, e) =>
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string path = files[0];

                    // Check if the path is valid based on whether we accept folders
                    bool isValid = (acceptFolders && Directory.Exists(path)) ||
                                  (!acceptFolders && File.Exists(path));

                    if (isValid)
                    {
                        onDropAction?.Invoke(path);
                    }
                }
            }

            e.Handled = true;
        };
    }

    private void RegisterTextBoxDragDrop(System.Windows.Controls.TextBox textBox, System.Windows.Input.ICommand command, bool acceptFolders)
    {
        if (textBox == null) return;

        textBox.AllowDrop = true;

        textBox.PreviewDragOver += (sender, e) =>
        {
            e.Effects = System.Windows.DragDropEffects.None;

            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }

            e.Handled = true;
        };

        textBox.Drop += (sender, e) =>
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string path = files[0];

                    // Check if the path is valid based on whether we accept folders
                    bool isValid = (acceptFolders && Directory.Exists(path)) ||
                                  (!acceptFolders && File.Exists(path));

                    if (isValid && command != null && command.CanExecute(path))
                    {
                        command.Execute(path);
                    }
                }
            }

            e.Handled = true;
        };
    }
}