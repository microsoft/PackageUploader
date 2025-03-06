// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace PackageUploader.UI;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }
}
