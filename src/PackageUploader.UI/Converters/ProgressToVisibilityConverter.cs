using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageUploader.UI.Converters
{
    public class ProgressToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int progress && parameter is string range)
            {
                var bounds = range.Split('-');
                if (bounds.Length == 2 &&
                    int.TryParse(bounds[0], out int min) &&
                    int.TryParse(bounds[1], out int max))
                {
                    return progress >= min && progress <= max ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
