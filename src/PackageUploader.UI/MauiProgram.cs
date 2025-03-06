// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.UI.Services;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;

namespace PackageUploader.UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Services
        builder.Services.AddSingleton<PackageModelService>();
        builder.Services.AddSingleton<PathConfigurationService>();

        // Register ViewModels
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<PackageCreationViewModel>();
        builder.Services.AddTransient<PackageUploadViewModel>();

        // Register Views with their ViewModels
        builder.Services.AddTransient<MainPageView>();
        builder.Services.AddTransient<PackageCreationView>();
        builder.Services.AddTransient<PackageUploadView>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
