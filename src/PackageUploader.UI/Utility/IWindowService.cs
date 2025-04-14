// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows.Controls;

namespace PackageUploader.UI.Utility
{
    /// <summary>
    /// Service for handling window and view navigation.
    /// </summary>
    public interface IWindowService
    {
        /// <summary>
        /// Navigate to a view created from a UserControl with a parameterless constructor.
        /// </summary>
        /// <typeparam name="T">Type of UserControl to navigate to</typeparam>
        void NavigateTo<T>() where T : System.Windows.Controls.UserControl, new();
        
        /// <summary>
        /// Navigate to a view created from the specified type.
        /// </summary>
        /// <param name="viewType">The type to create and navigate to</param>
        void NavigateTo(Type viewType);
        
        /// <summary>
        /// Show a dialog window.
        /// </summary>
        /// <typeparam name="T">Type of window to show as dialog</typeparam>
        void ShowDialog<T>() where T : class;
    }
}