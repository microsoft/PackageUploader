// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfButton = System.Windows.Controls.Button;
using WpfKey = System.Windows.Input.Key;
using WpfKeyboard = System.Windows.Input.Keyboard;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace PackageUploader.UI
{
    public partial class MainWindow : Window
    {
        private const double StandardWindowWidth = 1200;
        private const double StandardWindowHeight = 840;
        private const double CompactWindowWidth = 600;
        private const double CompactWindowHeight = 420;

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
            SizeChanged += (_, _) => UpdateTitleBarLayout();
            Loaded += (_, _) => UpdateTitleBarLayout();

            // Initial update of username display
            UpdateUsernameDisplay();

            // Set version display
            VersionText.Text = string.Format(UI.Resources.Strings.MainPage.VersionLabel, GetSimpleVersion());

            // Set initial window size before Show() so it opens at the correct dimensions
            if (_compactModeProvider.IsCompactMode)
            {
                ApplyCompactWindowSize();
            }
            else
            {
                ApplyStandardWindowSize();
            }

            // Sync icons with initial state
            UpdateThemeToggleIcon();
            UpdateCompactModeIcon();

#if !DEBUG
            ThemeToggleButton.Visibility = Visibility.Collapsed;
#endif
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
                string accountName = GetAccountDisplayName(_userLoggedInProvider.UserName);
                string tenantName = _userLoggedInProvider.TenantName?.Trim() ?? string.Empty;

                UserInitialsText.Text = GetInitials(_userLoggedInProvider.UserName);
                ProfileDisplayNameText.Text = accountName;
                ProfileTenantText.Text = tenantName;
                ProfileTenantText.Visibility = string.IsNullOrEmpty(tenantName)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                UserProfileButton.Visibility = Visibility.Visible;
                UpdateSignOutButtonState();
            }
            else
            {
                UserProfilePopup.IsOpen = false;
                UserInitialsText.Text = string.Empty;
                ProfileDisplayNameText.Text = string.Empty;
                ProfileTenantText.Text = string.Empty;
                UserProfileButton.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSignOutButtonState()
        {
            bool isOnMainPage = (ContentArea.Content as FrameworkElement)?.DataContext is MainPageViewModel;
            AccountSignOutButton.IsEnabled = isOnMainPage;

            string accountName = GetAccountDisplayName(_userLoggedInProvider.UserName);
            string tenantName = _userLoggedInProvider.TenantName?.Trim() ?? string.Empty;
            UserProfileButton.ToolTip = string.IsNullOrEmpty(tenantName)
                ? accountName
                : $"{accountName} - {tenantName}";
        }

        private static string GetAccountDisplayName(string? userName) =>
            string.IsNullOrWhiteSpace(userName)
                ? UI.Resources.Strings.MainPage.SignedInAccountFallback
                : userName.Trim();

        internal static string GetInitials(string? userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return "U";
            }

            string normalizedName = Regex.Replace(
                userName.Trim(),
                @"(?:\s*\([^()]*\))+\s*$",
                string.Empty).Trim();
            if (string.IsNullOrEmpty(normalizedName))
            {
                return "U";
            }

            string[] nameParts = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

        private void UserProfileButton_Click(object sender, RoutedEventArgs e)
        {
            HelpMenuPopup.IsOpen = false;
            UserProfilePopup.IsOpen = !UserProfilePopup.IsOpen;
            if (UserProfilePopup.IsOpen)
            {
                FocusPopupAction(AccountSignOutButton);
            }
        }

        private void AccountSignOutButton_Click(object sender, RoutedEventArgs e)
        {
            UserProfilePopup.IsOpen = false;
            _authenticationService.SignOut();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            UserProfilePopup.IsOpen = false;
            HelpMenuPopup.IsOpen = !HelpMenuPopup.IsOpen;
            if (HelpMenuPopup.IsOpen)
            {
                FocusPopupAction(DocumentationButton);
            }
        }

        private void FocusPopupAction(WpfButton button)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => button.Focus()));
        }

        private void HelpMenuPopup_KeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key == WpfKey.Escape)
            {
                HelpMenuPopup.IsOpen = false;
                e.Handled = true;
                return;
            }

            if (e.Key is WpfKey.Down or WpfKey.Up)
            {
                bool focusReportIssue = ReferenceEquals(WpfKeyboard.FocusedElement, DocumentationButton);
                (focusReportIssue ? ReportIssueButton : DocumentationButton).Focus();
                e.Handled = true;
            }
        }

        private void UserProfilePopup_KeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key == WpfKey.Escape)
            {
                UserProfilePopup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void HelpMenuPopup_Closed(object? sender, EventArgs e)
        {
            if (IsActive)
            {
                HelpButton.Focus();
            }
        }

        private void UserProfilePopup_Closed(object? sender, EventArgs e)
        {
            if (IsActive && UserProfileButton.Visibility == Visibility.Visible)
            {
                UserProfileButton.Focus();
            }
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
                    ApplyCompactWindowSize();
                }
                else
                {
                    ApplyStandardWindowSize();
                }
            }
        }

        private void ApplyStandardWindowSize()
        {
            Width = StandardWindowWidth;
            Height = GetStandardWindowHeight(SystemParameters.WorkArea.Height);
        }

        private void ApplyCompactWindowSize()
        {
            Width = CompactWindowWidth;
            Height = Math.Min(CompactWindowHeight, SystemParameters.WorkArea.Height);
        }

        internal static double GetStandardWindowHeight(double workAreaHeight) =>
            Math.Min(StandardWindowHeight, workAreaHeight);

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
            UpdateTitleBarLayout();

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

        private void UpdateTitleBarLayout()
        {
            EnvironmentBadge.Visibility = _compactModeProvider.IsCompactMode || ActualWidth < 900
                ? Visibility.Collapsed
                : Visibility.Visible;
            ApplicationTitleText.Visibility = ActualWidth > 0 && ActualWidth < 700
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}