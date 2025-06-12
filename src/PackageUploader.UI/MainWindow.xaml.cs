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

namespace PackageUploader.UI
{
    public partial class MainWindow : Window
    {
        private readonly UserLoggedInProvider _userLoggedInProvider;
        private readonly IAuthenticationService _authenticationService;

        public MainWindow(UserLoggedInProvider userLoggedInProvider, IAuthenticationService authenticationService)
        {
            InitializeComponent();
            _userLoggedInProvider = userLoggedInProvider;
            _authenticationService = authenticationService;

            // Subscribe to property changes to update the username display
            _userLoggedInProvider.PropertyChanged += UserLoggedInProvider_PropertyChanged;

            // Subscribe to ContentArea.Content changes
            RegisterContentAreaChangeHandler();

            // Initial update of username display
            UpdateUsernameDisplay();

            // Set version display
            VersionText.Text = string.Format(UI.Resources.Strings.MainPage.VersionLabel, GetSimpleVersion());
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
                return assemblyVersionAttribute.InformationalVersion.Split('+')[0];
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
    }
}