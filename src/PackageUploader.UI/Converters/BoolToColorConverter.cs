// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PackageUploader.UI.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var colorOptions = colors.Split(',');
                if (colorOptions.Length >= 2)
                {
                    string colorStr = boolValue ? colorOptions[0] : colorOptions[1];
                    
                    try
                    {
                        // Try to convert the color string to a WPF color
                        return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorStr);
                    }
                    catch
                    {
                        return Colors.Transparent;
                    }
                }
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
