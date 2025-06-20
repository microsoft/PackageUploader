// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageUploader.UI.Converters
{
    /// <summary>
    /// Converts a boolean value to a Visibility value.
    /// True becomes Visible, False becomes Collapsed by default.
    /// This behavior can be inverted using the parameter.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to a Visibility value.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter. If "Invert" or "true", inverts the conversion logic.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// Visibility.Visible if value is true (or false if inverted).
        /// Visibility.Collapsed if value is false (or true if inverted).
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool invert = IsInverted(parameter);
                return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a Visibility value back to a boolean value.
        /// </summary>
        /// <param name="value">The Visibility value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter. If "Invert" or "true", inverts the conversion logic.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// true if value is Visibility.Visible (or Visibility.Collapsed if inverted).
        /// false if value is Visibility.Collapsed (or Visibility.Visible if inverted).
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = IsInverted(parameter);
            bool result = value is Visibility v && v == Visibility.Visible;

            // If inverted, invert the result
            if (invert)
            {
                result = !result;
            }

            return result;
        }

        private static bool IsInverted(object parameter)
        {
            if (parameter == null)
            {
                return false;
            }
            
            var paramString = parameter.ToString();
            
            return paramString != null && (
                paramString.Equals("Invert", StringComparison.OrdinalIgnoreCase) || 
                paramString.Equals("true", StringComparison.OrdinalIgnoreCase));
        }
    }
}