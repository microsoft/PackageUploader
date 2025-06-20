// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageUploader.UI.Converters
{
    class PackageUploadingProgressStageToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PackageUploadingProgressStage stage && parameter is string stageName)
            {
                string[] startStopString = stageName.Split('-');
                if (startStopString.Length != 2)
                {
                    PackageUploadingProgressStage paramAsEnum;
                    if (Enum.TryParse<PackageUploadingProgressStage>(stageName, out paramAsEnum))
                    {
                        return stage == paramAsEnum ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                else
                {
                    PackageUploadingProgressStage startStage;
                    PackageUploadingProgressStage stopStage;
                    if (Enum.TryParse<PackageUploadingProgressStage>(startStopString[0], out startStage) && Enum.TryParse<PackageUploadingProgressStage>(startStopString[1], out stopStage))
                    {
                        return stage >= startStage && stage <= stopStage ? Visibility.Visible : Visibility.Collapsed;
                    }
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
