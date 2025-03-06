// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class PackageCreationView : ContentPage
{
    public PackageCreationView(PackageCreationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}