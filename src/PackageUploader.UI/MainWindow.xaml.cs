// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PackageUploader.UI
{
    public partial class MainWindow : Window
    {
        private readonly UserLoggedInProvider _userLoggedInProvider;
        private readonly IAuthenticationService _authenticationService;
        private readonly CompactModeProvider _compactModeProvider;

        public MainWindow(UserLoggedInProvider userLoggedInProvider, IAuthenticationService authenticationService, CompactModeProvider compactModeProvider)
        {
            InitializeComponent();
            _userLoggedInProvider = userLoggedInProvider;
            _authenticationService = authenticationService;
            _compactModeProvider = compactModeProvider;

            // Subscribe to property changes to update the username display
            _userLoggedInProvider.PropertyChanged += UserLoggedInProvider_PropertyChanged;

            // Subscribe to ContentArea.Content changes
            RegisterContentAreaChangeHandler();

            // Update maximize/restore icon when window state changes
            StateChanged += MainWindow_StateChanged;

            // Initial update of username display
            UpdateUsernameDisplay();

            // Set version display
            VersionText.Text = string.Format(UI.Resources.Strings.MainPage.VersionLabel, GetSimpleVersion());

            // Set initial window size before Show() so it opens at the correct dimensions
            if (_compactModeProvider.IsCompactMode)
            {
                Width = 600;
                Height = 420;
            }

            // Sync icons with initial state
            UpdateCompactModeIcon();
        }

        private void RegisterContentAreaChangeHandler()
        {
            DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl))
                .AddValueChanged(ContentArea, (s, e) => UpdateSignOutButtonState());
        }

        private void UserLoggedInProvider_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UserLoggedInProvider.UserName) ||
                e.PropertyName == nameof(UserLoggedInProvider.TenantName) ||
                e.PropertyName == nameof(UserLoggedInProvider.UserLoggedIn) ||
                e.PropertyName == nameof(UserLoggedInProvider.AccessToken))
            {
                UpdateUsernameDisplay();
            }
        }

        private void UpdateUsernameDisplay()
        {
            if (_userLoggedInProvider.UserLoggedIn)
            {
                if (string.IsNullOrEmpty(_userLoggedInProvider.TenantName))
                {
                    UserDisplayText.Text = _userLoggedInProvider.UserName;
                }
                else if (!string.IsNullOrEmpty(_userLoggedInProvider.UserName))
                {
                    UserDisplayText.Text = _userLoggedInProvider.UserName + " - " + _userLoggedInProvider.TenantName;
                }
                UserSignoutButton.Visibility = Visibility.Visible;
            }
            else
            {
                // Not logged in, hide button
                UserSignoutButton.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSignOutButtonState()
        {
            bool isOnMainPage = (ContentArea.Content as FrameworkElement)?.DataContext is MainPageViewModel;
            UserSignoutButton.IsEnabled = isOnMainPage;
            UserSignoutButton.ToolTip = isOnMainPage ? UI.Resources.Strings.MainPage.SignOutUser : null;
        }

        private static string GetSimpleVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            if (assemblyVersionAttribute is not null)
            {
                string version = assemblyVersionAttribute.InformationalVersion.Split('+')[0];
                if (version.Equals("1.0.0"))
                {
                    return assemblyVersionAttribute.InformationalVersion; // Return full version if it's the default 1.0.0
                }
                return version;
            }
            return assembly.GetName().Version?.ToString() ?? string.Empty;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Restore icon (two overlapping rectangles)
                MaximizeIcon.Data = System.Windows.Media.Geometry.Parse("M2,0 H10 V8 H2 Z M0,2 H8 V10 H0 Z");
            }
            else
            {
                // Maximize icon (single rectangle)
                MaximizeIcon.Data = System.Windows.Media.Geometry.Parse("M0,0 H10 V10 H0 V0");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void UserSignoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Sign out the user
            _authenticationService.SignOut();
        }

        private void GitHubIssuesButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the GitHub issues page
            Process.Start(new ProcessStartInfo("https://github.com/microsoft/PackageUploader/issues/new")
            {
                UseShellExecute = true
            });
        }

        private void CompactModeButton_Click(object sender, RoutedEventArgs e)
        {
            _compactModeProvider.IsCompactMode = !_compactModeProvider.IsCompactMode;
            UpdateCompactModeIcon();

            if (WindowState == WindowState.Normal)
            {
                if (_compactModeProvider.IsCompactMode)
                {
                    Width = 600;
                    Height = 420;
                }
                else
                {
                    Width = 1200;
                    Height = 800;
                }
            }
        }

        private void UpdateCompactModeIcon()
        {
            if (_compactModeProvider.IsCompactMode)
            {
                // Expand icon (lines spread apart)
                CompactModeIcon.Data = Geometry.Parse("M3,3 H13 M3,8 H13 M3,13 H13");
                CompactModeButton.ToolTip = UI.Resources.Strings.MainPage.CompactModeTooltipExpand;
            }
            else
            {
                // Compact icon (lines close together)
                CompactModeIcon.Data = Geometry.Parse("M3,5 H13 M3,8 H13 M3,11 H13");
                CompactModeButton.ToolTip = UI.Resources.Strings.MainPage.CompactModeTooltipCompact;
            }
        }
    }
}