using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageUploader.UI.Converters
{
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return !string.IsNullOrEmpty(stringValue) ^ IsInverted(parameter) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
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
