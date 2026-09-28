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
            UpdateThemeToggleIcon();
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
                UserInitialsText.Text = GetInitials(_userLoggedInProvider.UserName);
                UserSignoutButton.Visibility = Visibility.Visible;
                UpdateSignOutButtonState();
            }
            else
            {
                UserInitialsText.Text = string.Empty;
                UserSignoutButton.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSignOutButtonState()
        {
            bool isOnMainPage = (ContentArea.Content as FrameworkElement)?.DataContext is MainPageViewModel;
            UserSignoutButton.IsEnabled = isOnMainPage;

            string displayName = _userLoggedInProvider.UserName;
            if (!string.IsNullOrEmpty(_userLoggedInProvider.TenantName))
            {
                displayName += " - " + _userLoggedInProvider.TenantName;
            }

            UserSignoutButton.ToolTip = isOnMainPage
                ? displayName + Environment.NewLine + UI.Resources.Strings.MainPage.SignOutUser
                : displayName;
        }

        private static string GetInitials(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return "U";
            }

            string[] nameParts = userName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (nameParts.Length == 1)
            {
                return nameParts[0][..1].ToUpperInvariant();
            }

            return string.Concat(nameParts[0][0], nameParts[^1][0]).ToUpperInvariant();
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

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            HelpMenuPopup.IsOpen = !HelpMenuPopup.IsOpen;
        }

        private void DocumentationButton_Click(object sender, RoutedEventArgs e)
        {
            HelpMenuPopup.IsOpen = false;
            OpenUrl("https://github.com/microsoft/PackageUploader#xbox-game-package-manager");
        }

        private void GitHubIssuesButton_Click(object sender, RoutedEventArgs e)
        {
            HelpMenuPopup.IsOpen = false;
            OpenUrl("https://github.com/microsoft/PackageUploader/issues/new");
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url)
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

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ((App)System.Windows.Application.Current).ToggleThemeForTesting();
            UpdateThemeToggleIcon();
        }

        internal void UpdateThemeToggleIcon()
        {
            var app = (App)System.Windows.Application.Current;

            if (app.IsHighContrastThemeActive)
            {
                ThemeToggleButton.IsEnabled = false;
                ThemeToggleButton.ToolTip = "Theme testing is unavailable while Windows High Contrast is active";
                return;
            }

            ThemeToggleButton.IsEnabled = true;

            if (app.IsDarkThemeActive)
            {
                // Sun icon indicates the theme that will be activated.
                ThemeToggleIcon.Data = Geometry.Parse("M8,3 A5,5 0 1 0 8,13 A5,5 0 1 0 8,3 M8,0 V2 M8,14 V16 M0,8 H2 M14,8 H16 M2.3,2.3 L3.7,3.7 M12.3,12.3 L13.7,13.7 M13.7,2.3 L12.3,3.7 M3.7,12.3 L2.3,13.7");
                ThemeToggleButton.ToolTip = "Switch to light mode (temporary testing override)";
            }
            else
            {
                // Moon icon indicates the theme that will be activated.
                ThemeToggleIcon.Data = Geometry.Parse("M8,2 A6,6 0 1 0 14,8 A5,5 0 0 1 8,2");
                ThemeToggleButton.ToolTip = "Switch to dark mode (temporary testing override)";
            }
        }

        private void UpdateCompactModeIcon()
        {
            EnvironmentBadge.Visibility = _compactModeProvider.IsCompactMode
                ? Visibility.Collapsed
                : Visibility.Visible;

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