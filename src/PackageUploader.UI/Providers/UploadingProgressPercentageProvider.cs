// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PackageUploader.UI.Providers
{
    public partial class UploadingProgressPercentageProvider : INotifyPropertyChanged
    {
        private PackageUploadingProgress _uploadProgress = new();
        public PackageUploadingProgress UploadProgress
        {
            get => _uploadProgress;
            set
            {
                if (!_uploadProgress.Equals(value))
                {
                    _uploadProgress = value;
                    UploadStage = _uploadProgress.Stage;
                    UploadingProgressPercentage = _uploadProgress.Percentage;
                    OnPropertyChanged();
                }
            }
        }

        private PackageUploadingProgressStage _uploadStage;
        public PackageUploadingProgressStage UploadStage
        {
            get => _uploadStage;
            set
            {
                if (_uploadStage != value)
                {
                    _uploadStage = value;
                    OnPropertyChanged(nameof(UploadStage));
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
                    OnPropertyChanged(nameof(UploadingProgressPercentage));
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
    }
}
