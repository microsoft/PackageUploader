// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.ViewModel;
using System.ComponentModel;
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
        }

        private void RegisterContentAreaChangeHandler()
        {
            DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl))
                .AddValueChanged(ContentArea, (s, e) => UpdateSignOutButtonState());
        }

        private void UserLoggedInProvider_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UserLoggedInProvider.UserName) || 
                e.PropertyName == nameof(UserLoggedInProvider.UserLoggedIn) ||
                e.PropertyName == nameof(UserLoggedInProvider.AccessToken))
            {
                UpdateUsernameDisplay();
            }
        }

        private void UpdateUsernameDisplay()
        {
            if (_userLoggedInProvider.UserLoggedIn && !string.IsNullOrEmpty(_userLoggedInProvider.UserName))
            {
                UserDisplayText.Text = _userLoggedInProvider.UserName;
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
            UserSignoutButton.ToolTip = isOnMainPage ? "Sign Out User" : null;
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
    }
}