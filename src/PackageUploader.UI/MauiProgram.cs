﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi;
using PackageUploader.FileLogger;
using PackageUploader.UI.Services;
using PackageUploader.UI.View;
using PackageUploader.UI.ViewModel;
using static PackageUploader.ClientApi.IngestionExtensions;

namespace PackageUploader.UI;

public static class MauiProgram
{

    private const string LogTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Services
        builder.Services.AddSingleton<PackageModelService>();
        builder.Services.AddSingleton<PathConfigurationService>();
        builder.Services.AddPackageUploaderService(AuthenticationMethod.Default);

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

        builder.Logging.AddSimpleFile(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = LogTimestampFormat;
        }, file =>
        {
            file.Path = Path.Combine(Path.GetTempPath(), $"PackageUploader_UI_{DateTime.Now:yyyyMMddHHmmss}.log");
            file.Append = true;
        });

        return builder.Build();
    }
}
