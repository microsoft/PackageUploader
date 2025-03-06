// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

namespace PackageUploader.UI.Converters;

/// <summary>
/// Converts a boolean value to its inverse (true becomes false, false becomes true)
/// Used for binding visibility or enablement to the inverse of a boolean property
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value;
    }
}
