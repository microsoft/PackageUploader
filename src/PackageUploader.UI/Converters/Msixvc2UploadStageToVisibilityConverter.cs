// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Model;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageUploader.UI.Converters;

internal class Msixvc2UploadStageToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Msixvc2UploadStage stage && parameter is string stageName)
        {
            string[] range = stageName.Split('-');
            if (range.Length == 2)
            {
                if (Enum.TryParse<Msixvc2UploadStage>(range[0], out var start) &&
                    Enum.TryParse<Msixvc2UploadStage>(range[1], out var stop))
                {
                    return stage >= start && stage <= stop ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            else if (Enum.TryParse<Msixvc2UploadStage>(stageName, out var exact))
            {
                return stage == exact ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
