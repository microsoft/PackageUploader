// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;

namespace PackageUploader.UI.Utility
{
    /// <summary>
    /// Implementation of the IWindowService that handles navigation and dialog management.
    /// </summary>
    public class WindowService : IWindowService
    {
        private readonly ContentControl _contentControl;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the WindowService class with a content control for navigation.
        /// </summary>
        /// <param name="contentControl">Content control that will host the navigated views</param>
        /// <param name="serviceProvider">Service provider for resolving dependencies</param>
        public WindowService(ContentControl contentControl, IServiceProvider serviceProvider)
        {
            _contentControl = contentControl ?? throw new ArgumentNullException(nameof(contentControl));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc/>
        public void NavigateTo<T>() where T : System.Windows.Controls.UserControl, new()
        {
            _contentControl.Content = new T();
        }

        /// <inheritdoc/>
        public void NavigateTo(Type viewType)
        {
            if (!typeof(UIElement).IsAssignableFrom(viewType))
            {
                throw new ArgumentException($"Type {viewType.Name} must inherit from UIElement");
            }

            UIElement? view = null;
            try
            {
                // Try to resolve the view from the service provider
                view = _serviceProvider.GetService(viewType) as UIElement;
                
                // If not registered in DI, try to create it with Activator as fallback
                if (view == null)
                {
                    view = Activator.CreateInstance(viewType) as UIElement;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not create an instance of {viewType.Name}: {ex.Message}", ex);
            }
            
            if (view == null)
            {
                throw new InvalidOperationException($"Could not create an instance of {viewType.Name}");
            }
            
            _contentControl.Content = view;
        }

        /// <inheritdoc/>
        public void ShowDialog<T>() where T : class
        {
            Type viewType = typeof(T);
            
            if (!typeof(Window).IsAssignableFrom(viewType))
            {
                throw new InvalidOperationException($"{viewType.Name} must be a Window to show as dialog");
            }
            
            Window? dialog = null;
            try
            {
                // Try to resolve the dialog from the service provider
                dialog = _serviceProvider.GetService(viewType) as Window;
                
                // If not registered in DI, try to create it with Activator as fallback
                if (dialog == null)
                {
                    dialog = Activator.CreateInstance(viewType) as Window;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not create an instance of {viewType.Name}: {ex.Message}", ex);
            }
            
            if (dialog == null)
            {
                throw new InvalidOperationException($"Could not create an instance of {viewType.Name}");
            }
            
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
        }
    }
}