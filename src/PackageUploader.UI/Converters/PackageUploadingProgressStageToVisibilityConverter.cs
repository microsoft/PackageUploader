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
            if (value is PackageUploadingProgressStages stage && parameter is string stageName) // thanks copilot
            {
                string[] startStopString = stageName.Split('-');
                if (startStopString.Length != 2)
                {



                    PackageUploadingProgressStages paramAsEnum;
                    if (Enum.TryParse<PackageUploadingProgressStages>(stageName, out paramAsEnum))
                    {
                        return stage == paramAsEnum ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                else
                { // up, thanks copilot
                    PackageUploadingProgressStages startStage;
                    PackageUploadingProgressStages stopStage;
                    if (Enum.TryParse<PackageUploadingProgressStages>(startStopString[0], out startStage) && Enum.TryParse<PackageUploadingProgressStages>(startStopString[1], out stopStage))
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
