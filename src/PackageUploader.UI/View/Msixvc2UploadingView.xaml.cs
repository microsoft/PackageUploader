// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class Msixvc2UploadingView : System.Windows.Controls.UserControl
{
    private readonly Msixvc2UploadingViewModel _viewModel;

    public Msixvc2UploadingView(Msixvc2UploadingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        // First load only: OnAppearing starts the upload, and a re-attached instance raising Loaded
        // again would otherwise start a second one.
        this.OnFirstLoad(() => _viewModel.OnAppearing());
    }
}
