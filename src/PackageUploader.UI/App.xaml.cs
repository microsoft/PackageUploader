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
using System;
using Microsoft.Win32;
using System.Windows.Media;

namespace PackageUploader.UI;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    private static readonly string LogFilePath = System.IO.Path.Combine(
                           System.IO.Path.GetTempPath(),
                           $"PackageUploader_UI_{DateTime.Now:yyyyMMddHHmmss}.log");

    private const string LightTheme = "Resources/Styles/Colors.Light.xaml";
    private const string DarkTheme = "Resources/Styles/Colors.Dark.xaml";
    private const string HighContrastTheme = "Resources/Styles/Colors.HighContrast.xaml";

    public static string GetLogFilePath() => LogFilePath;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register services
                services.AddPackageUploaderService(IngestionExtensions.AuthenticationMethod.CacheableBrowser);
                services.AddSingleton<IAuthenticationService, AuthenticationService>();
                
                // Register providers
                services.AddSingleton<PackageModelProvider>();
                services.AddSingleton<PathConfigurationProvider>();
                services.AddSingleton<UserLoggedInProvider>();
                services.AddSingleton<PackingProgressPercentageProvider>();
                services.AddSingleton<UploadingProgressPercentageProvider>();
                services.AddSingleton<ErrorModelProvider>();

                // Register ViewModels
                services.AddSingleton<MainPageViewModel>();
                services.AddSingleton<PackageCreationViewModel>();
                services.AddSingleton<PackageUploadViewModel>();
                services.AddSingleton<PackagingProgressViewModel>();
                services.AddSingleton<PackagingFinishedViewModel>();
                services.AddSingleton<PackageUploadingViewModel>();
                services.AddSingleton<UploadingFinishedViewModel>();
                services.AddSingleton<ErrorScreenViewModel>();

                // Register Views
                services.AddTransient<MainPageView>();
                services.AddTransient<PackageCreationView>();
                services.AddTransient<PackageUploadView>();
                services.AddTransient<PackagingProgressView>();
                services.AddTransient<PackagingFinishedView>();
                services.AddTransient<PackageUploadingView>();
                services.AddTransient<UploadingFinishedView>();
                services.AddTransient<ErrorPageView>();

                // Register the main window
                services.AddSingleton<MainWindow>();
                
                // Register WindowService (will be initialized after MainWindow is created)
                services.AddSingleton<IWindowService>(provider => {
                    var mainWindow = provider.GetRequiredService<MainWindow>();
                    return mainWindow.FindName("ContentArea") is not ContentControl contentControl
                        ? throw new InvalidOperationException("Failed to find ContentArea control in MainWindow")
                        : (IWindowService)new WindowService(contentControl, provider);
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
                    fileOptions.Path = LogFilePath;
                });
            })
            .Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        FrameworkElement.StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata
        {
            DefaultValue = TryFindResource(typeof(Window)) ?? new Style()
        });

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        // Navigate to the initial view
        var windowService = _host.Services.GetRequiredService<Utility.IWindowService>();
        windowService.NavigateTo(typeof(MainPageView));

        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        ApplyTheme();
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.Color)
        {
            // Theme or high contrast setting may have changed
            Dispatcher.Invoke(ApplyTheme);
        }
    }

    private void ApplyTheme()
    {
        string themeToApply = GetSystemThemeResource();
        var dicts = Resources.MergedDictionaries;
        // Remove any of our theme dictionaries
        for (int i = dicts.Count - 1; i >= 0; i--)
        {
            var src = dicts[i].Source?.ToString();
            if (src != null && (src.EndsWith(LightTheme) || src.EndsWith(DarkTheme) || src.EndsWith(HighContrastTheme)))
                dicts.RemoveAt(i);
        }
        dicts.Insert(0, new ResourceDictionary { Source = new Uri(themeToApply, UriKind.Relative) });
    }

    private static string GetSystemThemeResource()
    {
        // High Contrast detection
        if (SystemParameters.HighContrast)
        {
            return HighContrastTheme;
        }

        // Light/Dark detection (Windows 10/11)
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var appsUseLightTheme = key.GetValue("AppsUseLightTheme");
                if (appsUseLightTheme is int value)
                {
                    return value == 0 ? DarkTheme : LightTheme;
                }
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., registry access issues)
            var logger = ((App)Current)._host.Services.GetRequiredService<ILogger<App>>();
            logger.LogError(ex, "Error accessing registry.");
        }
        return DarkTheme;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        _host.Dispose();
        base.OnExit(e);
    }
}
