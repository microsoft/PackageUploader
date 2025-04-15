// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;
using PackageUploader.UI.View;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PackageUploader.UI.ViewModel;
using PackageUploader.ClientApi;
using Microsoft.Extensions.Logging;
using PackageUploader.FileLogger;
using PackageUploader.UI.Providers;
using System.Windows.Controls;
using PackageUploader.UI.Utility;

namespace PackageUploader.UI;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddPackageUploaderService();
                
                // Register providers
                services.AddSingleton<PackageModelProvider>();
                services.AddSingleton<PathConfigurationProvider>();
                services.AddSingleton<UserLoggedInProvider>();
                
                // Register ViewModels
                services.AddTransient<MainPageViewModel>();
                services.AddTransient<PackageCreationViewModel>();
                services.AddTransient<PackageUploadViewModel>();
                
                // Register Views
                services.AddTransient<MainPageView>();
                services.AddTransient<PackageCreationView>();
                services.AddTransient<PackageUploadView>();
                services.AddTransient<PackageCreationView2>();
                services.AddTransient<PackagingProgressView>();

                // Register the main window
                services.AddSingleton<MainWindow>();
                
                // Register WindowService (will be initialized after MainWindow is created)
                services.AddSingleton<IWindowService>(provider => {
                    var mainWindow = provider.GetRequiredService<MainWindow>();
                    var contentControl = mainWindow.FindName("ContentArea") as ContentControl;
                    if (contentControl == null)
                    {
                        throw new InvalidOperationException("Failed to find ContentArea control in MainWindow");
                    }
                    return new WindowService(contentControl, provider);
                });
            })
            .ConfigureLogging((context, logging) => 
            {
                logging.AddDebug();
                
                // Use SimpleFile formatter with a custom log path
                logging.AddSimpleFile(options => {
                    // Configure formatter options if needed
                }, fileOptions => {
                    // Set custom log file path
                    fileOptions.Path = System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(), 
                        $"PackageUploader_UI_{DateTime.Now:yyyyMMddHHmmss}.log");
                });
            })
            .Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        // Navigate to the initial view
        var windowService = _host.Services.GetRequiredService<Utility.IWindowService>();
        windowService.NavigateTo(typeof(MainPageView));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host.Dispose();
        base.OnExit(e);
    }
}
