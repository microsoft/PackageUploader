// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.ClientApi.Models;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using System.ComponentModel;
using System.Windows.Input;

namespace PackageUploader.UI.ViewModel
{
    public partial class PackageUploadingViewModel : BaseViewModel
    {
        public readonly UploadingProgressPercentageProvider _uploadingProgressPercentageProvider;
        private readonly IWindowService _windowService;

        public int PackageUploadPercentage
        {
            get => _uploadingProgressPercentageProvider.UploadingProgressPercentage;
            set
            {
                if (_uploadingProgressPercentageProvider.UploadingProgressPercentage != value)
                {
                    _uploadingProgressPercentageProvider.UploadingProgressPercentage = value;
                    OnPropertyChanged(nameof(PackageUploadPercentage));
                }
            }
        }
        public PackageUploadingProgressStage UploadStage {
            get => _uploadingProgressPercentageProvider.UploadStage;
            set
            {
                if(_uploadingProgressPercentageProvider.UploadStage != value)
                {
                    _uploadingProgressPercentageProvider.UploadStage = value;
                    OnPropertyChanged(nameof(UploadStage));
                }
            }
        }

        public ICommand CancelUploadCommand { get; }


        public PackageUploadingViewModel(UploadingProgressPercentageProvider uploadingProgressPercentageProvider, IWindowService windowService)
        {
            _uploadingProgressPercentageProvider = uploadingProgressPercentageProvider;
            _uploadingProgressPercentageProvider.PropertyChanged += UploadingProgressUpdate;
            _windowService = windowService;

            CancelUploadCommand = new RelayCommand(CancelUpload);
        }

        public void UploadingProgressUpdate(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UploadingProgressPercentageProvider.UploadingProgressPercentage))
            {
                OnPropertyChanged(nameof(PackageUploadPercentage));
            }
            else if (e.PropertyName == nameof(UploadingProgressPercentageProvider.UploadStage))
            {
                OnPropertyChanged(nameof(UploadStage));
            }
            else if (e.PropertyName == nameof(UploadingProgressPercentageProvider.UploadingCancelled))
            {
                OnPropertyChanged(nameof(CancelUploadCommand));
            }
        }

        private void CancelUpload()
        {
            _uploadingProgressPercentageProvider.UploadingCancelled = true;

            //System.Windows.Application.Current.Dispatcher.Invoke(() =>
            //{
                _windowService.NavigateTo(typeof(PackageUploadView));
            //});
        }
    }
}
