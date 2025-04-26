using PackageUploader.ClientApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PackageUploader.UI.Providers
{
    public partial class UploadingProgressPercentageProvider : INotifyPropertyChanged
    {
        private PackageUploadingProgressStages _uploadStage = PackageUploadingProgressStages.NotStarted;
        public PackageUploadingProgressStages UploadStage
        {
            get => _uploadStage;
            set
            {
                if (_uploadStage != value)
                {
                    _uploadStage = value;
                    UploadingProgressPercentage = (int)(StageToPercentage(value));
                    OnPropertyChanged();
                }
            }
        }
        private int _uploadingProgresPercentage;
        public int UploadingProgressPercentage
        {
            get => _uploadingProgresPercentage;
            set
            {
                if (_uploadingProgresPercentage != value)
                {


                    _uploadingProgresPercentage = value;
                    UploadStage = PercentageToStage(value);
                    OnPropertyChanged();
                }
            }
        }

        private bool _uploadingCancelled;
        public bool UploadingCancelled
        {
            get => _uploadingCancelled;
            set
            {
                if (_uploadingCancelled != value)
                {
                    _uploadingCancelled = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private double StageToPercentage(PackageUploadingProgressStages stage)
        {
            return stage switch
            {
                PackageUploadingProgressStages.NotStarted => 0,
                PackageUploadingProgressStages.ComputingDeltas => 10,
                PackageUploadingProgressStages.UploadingPackage => 20,
                PackageUploadingProgressStages.ProcessingPackage => 80,
                PackageUploadingProgressStages.UploadingSupplementalFiles => 90,
                PackageUploadingProgressStages.Done => 100,
                _ => throw new NotImplementedException(),
            };
        }
        // thanks copilot
        private PackageUploadingProgressStages PercentageToStage(double percentage)
        {
            return percentage switch
            {
                < 10 => PackageUploadingProgressStages.NotStarted,
                < 20 => PackageUploadingProgressStages.ComputingDeltas,
                < 80 => PackageUploadingProgressStages.UploadingPackage,
                < 90 => PackageUploadingProgressStages.ProcessingPackage,
                < 99 => PackageUploadingProgressStages.UploadingSupplementalFiles,
                <= 100 => PackageUploadingProgressStages.Done,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
