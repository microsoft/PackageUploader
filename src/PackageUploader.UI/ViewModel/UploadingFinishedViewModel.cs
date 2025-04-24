using Microsoft.Extensions.Logging;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public ICommand CloseCommand { get; }
        public ICommand ViewLogsCommand { get; }
        public ICommand ViewInPartnerCenterCommand { get; }

        public UploadingFinishedViewModel(
            IWindowService windowService,
            PackageModelProvider packageModelProvider,
            PathConfigurationProvider pathConfigurationService,
            ILogger<PackagingFinishedViewModel> logger)
        {
            _windowService = windowService;
            _packageModelProvider = packageModelProvider;
            _pathConfigurationService = pathConfigurationService;
            _logger = logger;
            
            CloseCommand = new RelayCommand(OnClose);
            ViewLogsCommand = new RelayCommand(OnViewLogs);
            ViewInPartnerCenterCommand = new RelayCommand(OnViewInPartnerCenter);

            // Golden Path: Packaging->Uploading, Need to figure it out for XVC->Uploading
            //_gameConfigModel = new PartialGameConfigModel(_packageModelProvider.Package.GameConfigFilePath);
            PackagePreviewImage = _packageModelProvider.Package.PackagePreviewImage; //LoadBitmapImage(_gameConfigModel.ShellVisuals.Square150x150Logo);
            VersionNum = _packageModelProvider.Package.Version; //_gameConfigModel.Identity.Version;
            StoreId = _packageModelProvider.Package.BigId; //_gameConfigModel.StoreId;

            FileInfo packageInfo = new FileInfo(_packageModelProvider.Package.PackageFilePath);
            PackageFileName = packageInfo.Name;
            PackageSize = TranslateFileSize(packageInfo.Length);
        }

        public void OnClose()
        {
            System.Windows.Application.Current.Shutdown();
            /*System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.NavigateTo(typeof(MainPageView));
            });*/
        }

        public void OnViewLogs()
        {
            string logPath = _packageModelProvider.PackagingLogFilepath;
            Process.Start("explorer.exe", $"/select, \"{logPath}\"");
        }

        public void OnViewInPartnerCenter()
        {
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

        private static BitmapImage LoadBitmapImage(string imagePath)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(imagePath);
            image.EndInit();
            return image;
        }
    }
}
