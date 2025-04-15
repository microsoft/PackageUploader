// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Providers;
using System;
using System.ComponentModel;
using System.Windows;

namespace PackageUploader.UI
{
    public partial class MainWindow : Window
    {
        private readonly UserLoggedInProvider _userLoggedInProvider;

        public MainWindow(UserLoggedInProvider userLoggedInProvider)
        {
            InitializeComponent();
            _userLoggedInProvider = userLoggedInProvider;
            
            // Subscribe to property changes to update the username display
            _userLoggedInProvider.PropertyChanged += UserLoggedInProvider_PropertyChanged;
            
            // Initial update of username display
            UpdateUsernameDisplay();
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
                UserDisplayText.Visibility = Visibility.Visible;
            }
            else
            {
                // Not logged in, hide name
                UserDisplayText.Visibility = Visibility.Collapsed;
            }
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
    }
}