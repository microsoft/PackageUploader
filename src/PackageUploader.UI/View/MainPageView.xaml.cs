// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;
using System.Windows.Controls;

namespace PackageUploader.UI.View;

public partial class MainPageView : System.Windows.Controls.UserControl
{
    private readonly MainPageViewModel _viewModel;

    public MainPageView(MainPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        // Refresh the UI once the control is loaded. Loaded can be raised again on a re-attached
        // instance, so this runs on the first load only.
        this.OnFirstLoad(() => _viewModel.OnAppearing());
    }
}