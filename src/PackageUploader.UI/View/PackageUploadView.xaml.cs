// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class PackageUploadView : ContentPage
{
    public PackageUploadView(PackageUploadViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}