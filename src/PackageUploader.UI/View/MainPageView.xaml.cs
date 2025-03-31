// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI.View;

public partial class MainPageView : ContentPage
{
    public MainPageView(ViewModel.MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Call the view model's OnAppearing method to refresh UI
        if (BindingContext is ViewModel.MainPageViewModel viewModel)
        {
            viewModel.OnAppearing();
        }
    }
}