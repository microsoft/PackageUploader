// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.UI.View;

public partial class MainPageView : ContentPage
{
    public MainPageView(ViewModel.MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}