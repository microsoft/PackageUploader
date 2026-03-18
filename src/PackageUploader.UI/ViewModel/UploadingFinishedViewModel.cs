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

        private string _productName = string.Empty;
        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        private string _destination = string.Empty;
        public string Destination
        {
            get => _destination;
            set => SetProperty(ref _destination, value);
        }

        private string _market = string.Empty;
        public string Market
        {
            get => _market;
            set => SetProperty(ref _market, value);
        }

        private string _packageIdentityName = string.Empty;
        public string PackageIdentityName
        {
            get => _packageIdentityName;
            set => SetProperty(ref _packageIdentityName, value);
        }

        private string _storeId = string.Empty;
        public string StoreId
        {
            get => _storeId;
            set => SetProperty(ref _storeId, value);
        }

        private string _folderSize = string.Empty;
        public string FolderSize
        {
            get => _folderSize;
            set => SetProperty(ref _folderSize, value);
        }

        private string _uploadSize = string.Empty;
        public string UploadSize
        {
            get => _uploadSize;
            set => SetProperty(ref _uploadSize, value);
        }

        private bool _hasUploadSize;
        public bool HasUploadSize
        {
            get => _hasUploadSize;
            set => SetProperty(ref _hasUploadSize, value);
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
            StoreId = _packageModelProvider.Package.BigId;
            PackageType = _packageModelProvider.Package.PackageType;

            string packageName = _packageModelProvider.Package.PackageName;
            string packageFilePath = _packageModelProvider.Package.PackageFilePath;
            if (!string.IsNullOrEmpty(packageName))
            {
                ProductName = packageName;
            }
            else if (!string.IsNullOrEmpty(packageFilePath) && File.Exists(packageFilePath))
            {
                ProductName = new FileInfo(packageFilePath).Name;
            }
            else
            {
                ProductName = "Loose content upload";
            }

            Destination = _packageModelProvider.Package.Destination;
            Market = _packageModelProvider.Package.Market;

            PackageIdentityName = _packageModelProvider.Package.PackageIdentityName;

            FolderSize = _packageModelProvider.Package.FolderSize;

            string uploadSize = _packageModelProvider.Package.UploadSize;
            if (!string.IsNullOrEmpty(uploadSize))
            {
                UploadSize = uploadSize;
                HasUploadSize = true;
            }
            else
            {
                HasUploadSize = false;
            }
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
    }
}
