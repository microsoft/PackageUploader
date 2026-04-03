// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Windows.Data;

namespace PackageUploader.UI.Converters;

public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double actualWidth && !double.IsNaN(actualWidth) && !double.IsInfinity(actualWidth) &&
            parameter is string paramStr &&
            double.TryParse(paramStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var percentage))
        {
            return actualWidth * percentage;
        }
        return System.Windows.DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
