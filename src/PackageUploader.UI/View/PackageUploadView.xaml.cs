// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.ViewModel;
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
    }
}