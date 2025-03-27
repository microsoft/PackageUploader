// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

namespace PackageUploader.UI.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var colorOptions = colors.Split(',');
                if (colorOptions.Length >= 2)
                {
                    return boolValue ? colorOptions[0] : colorOptions[1];
                }
            }

            return Colors.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
