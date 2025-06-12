// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace PackageUploader.UI.ViewModel
{
    public partial class PackagingProgressViewModel : BaseViewModel
    {
        public readonly PackageModelProvider _packageModelProvider;
        public readonly PackingProgressPercentageProvider _packingProgressPercentageProvider;
        private readonly IWindowService _windowService;
        private readonly IProcessStarterService _processStarterService;

        public int PackingProgressPercentage
        {
            get => _packingProgressPercentageProvider.PackingProgressPercentage;
            set
            {
                if (_packingProgressPercentageProvider.PackingProgressPercentage != value)
                {
                    _packingProgressPercentageProvider.PackingProgressPercentage = value;
                    OnPropertyChanged(nameof(PackingProgressPercentage));
                }
            }
        }

        public ICommand ViewLogsCommand { get; }
        public ICommand CancelCreationCommand { get; }


        public PackagingProgressViewModel(PackingProgressPercentageProvider packingProgressPercentageProvider, PackageModelProvider packageModelProvider, IWindowService windowService, IProcessStarterService processStarterService)
        {
            _packingProgressPercentageProvider = packingProgressPercentageProvider;
            _packingProgressPercentageProvider.PropertyChanged += PackagingProgressUpdate;
            _packageModelProvider = packageModelProvider;
            _windowService = windowService;
            _processStarterService = processStarterService;

            ViewLogsCommand = new RelayCommand(ViewLogs);
            CancelCreationCommand = new RelayCommand(CancelCreation);
        }

        public void PackagingProgressUpdate(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PackingProgressPercentageProvider.PackingProgressPercentage))
            {
                OnPropertyChanged(nameof(PackingProgressPercentage));
            }
        }

        private void CancelCreation()
        {
            _packingProgressPercentageProvider.PackingCancelled = true;

            //System.Windows.Application.Current.Dispatcher.Invoke(() =>
            //{
                _windowService.NavigateTo(typeof(PackageCreationView));
            //});
        }

        private void ViewLogs()
        {
            string logPath = _packageModelProvider.PackagingLogFilepath;
            _processStarterService.Start("explorer.exe", $"/select, \"{logPath}\"");
        }
    }
}
