// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.ViewModel;
using System.Windows.Controls;

namespace PackageUploader.UI.View;

public partial class PackageCreationView : System.Windows.Controls.UserControl
{
    public PackageCreationView(PackageCreationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}