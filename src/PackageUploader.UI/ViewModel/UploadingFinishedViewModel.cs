// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PackageUploader.UI.ViewModel
{
    public partial class UploadingFinishedViewModel : BaseViewModel
    {
        private readonly IWindowService _windowService;
        private readonly PackageModelProvider _packageModelProvider;
        private readonly PathConfigurationProvider _pathConfigurationService;
        private readonly ILogger<PackagingFinishedViewModel> _logger;
        private readonly IProcessStarterService _processStarterService;

        private BitmapImage? _packagePreviewImage = null;
        public BitmapImage? PackagePreviewImage
        {
            get => _packagePreviewImage;
            set => SetProperty(ref _packagePreviewImage, value);
        }

        private string _packageFileName = string.Empty;
        public string PackageFileName
        {
            get => _packageFileName;
            set => SetProperty(ref _packageFileName, value);
        }

        private string _packageSize = string.Empty;
        public string PackageSize
        {
            get => _packageSize;
            set => SetProperty(ref _packageSize, value);
        }

        private string _versionNum = string.Empty;
        public string VersionNum
        {
            get => _versionNum;
            set => SetProperty(ref _versionNum, value);
        }

        private string _storeId = string.Empty;
        public string StoreId
        {
            get => _storeId;
            set => SetProperty(ref _storeId, value);
        }

        private string _packageType = string.Empty;
        public string PackageType
        {
            get => _packageType;
            set => SetProperty(ref _packageType, value);
        }

        public ICommand HomeCommand { get; }
        public ICommand ViewLogsCommand { get; }
        public ICommand ViewInPartnerCenterCommand { get; }

        public UploadingFinishedViewModel(
            IWindowService windowService,
            PackageModelProvider packageModelProvider,
            PathConfigurationProvider pathConfigurationService,
            ILogger<PackagingFinishedViewModel> logger,
            IProcessStarterService processStarterService)
        {
            _windowService = windowService;
            _packageModelProvider = packageModelProvider;
            _pathConfigurationService = pathConfigurationService;
            _logger = logger;
            _processStarterService = processStarterService;

            HomeCommand = new RelayCommand(OnHome);
            ViewLogsCommand = new RelayCommand(OnViewLogs);
            ViewInPartnerCenterCommand = new RelayCommand(OnViewInPartnerCenter);
        }

        public void OnAppearing()
        {
            PackagePreviewImage = _packageModelProvider.Package.PackagePreviewImage;
            VersionNum = _packageModelProvider.Package.Version;
            StoreId = _packageModelProvider.Package.BigId;

            FileInfo packageInfo = new(_packageModelProvider.Package.PackageFilePath);
            PackageFileName = packageInfo.Name;
            PackageSize = TranslateFileSize(packageInfo.Length);
            PackageType = _packageModelProvider.Package.PackageType;
        }

        public void OnHome()
        {
           //System.Windows.Application.Current.Dispatcher.Invoke(() =>
           //{
               _windowService.NavigateTo(typeof(MainPageView));
           //});
        }

        public void OnViewLogs()
        {
            string logPath = App.GetLogFilePath();
            _processStarterService.Start("explorer.exe", $"/select, \"{logPath}\"");
        }

        public void OnViewInPartnerCenter()
        {
            string branchId = _packageModelProvider.Package.BranchId;
            string partnerCenterUrl = $"https://partner.microsoft.com/en-us/dashboard/products/{StoreId}/packages/{branchId}";
            _processStarterService.Start(new ProcessStartInfo(partnerCenterUrl) { UseShellExecute = true });
        }

        private static string TranslateFileSize(long size)
        {
            if (size > 1024 * 1024 * 1024)
            {
                return $"{(size / 1024d / 1024d / 1024d):F2} GB"; // Format to 2 decimal places
            }
            else if (size > 1024 * 1024)
            {
                return $"{size / 1024 / 1024} MB"; // No need to include decimal places if the package is less than 1 GB.
            }
            else if (size > 1024)
            {
                return $"{size / 1024} KB";
            }
            else
            {
                return $"{size} B";
            }
        }
    }
}
