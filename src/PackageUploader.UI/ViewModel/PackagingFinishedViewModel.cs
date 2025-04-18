using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PackageUploader.UI.ViewModel
{
    public partial class PackagingFinishedViewModel : BaseViewModel
    {
        private readonly IWindowService _windowService;
        private readonly PackageModelProvider _packageModelProvider;
        private readonly GameConfigModel _gameConfigModel;

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
        private string _appId = string.Empty;
        public string AppId
        {
            get => _appId;
            set => SetProperty(ref _appId, value);
        }

        public ICommand InstallGameCommand;
        public ICommand ViewPackageCommand;
        public ICommand CloseCommand;
        public ICommand ConfigureUploadCommand;

        public PackagingFinishedViewModel(IWindowService windowService, PackageModelProvider packageModelProvider)
        {
            _windowService = windowService;
            _packageModelProvider = packageModelProvider;
            _gameConfigModel = new GameConfigModel(_packageModelProvider.Package.GameConfigFilePath);
            PackagePreviewImage = LoadBitmapImage(_gameConfigModel.ShellVisuals.StoreLogo);
            VersionNum = _gameConfigModel.Identity.Version;
            AppId = _gameConfigModel.MSAAppId;


            InstallGameCommand = new RelayCommand(InstallGame);
            ViewPackageCommand = new RelayCommand(ViewPackage);
            CloseCommand = new RelayCommand(Close);
            ConfigureUploadCommand = new RelayCommand(ConfigureUpload);

            FileInfo packageInfo = new FileInfo(_packageModelProvider.Package.PackageFilePath);
            PackageFileName = packageInfo.Name; //Path.GetFileName(_packageModelProvider.Package.PackageFilePath);
            PackageSize = TranslateFileSize(packageInfo.Length);
        }

        public void InstallGame()
        {
            //_windowService.ShowDialog<InstallGameView>();
        }

        public void ViewPackage()
        {
            // Dunno if this would work at all, Copilot wrote this
            System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{_packageModelProvider.Package.PackageFilePath}\"");
        }

        public void Close()
        {
            // TODO: Close
        }
        public void ConfigureUpload()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.NavigateTo(typeof(PackageUploadView));
            });
        }


        private static string TranslateFileSize(long size)
        {
            // thanks copilot
            if (size > 1024 * 1024 * 1024)
            {
                return $"{size / 1024 / 1024 / 1024} GB";
            }
            else if (size > 1024 * 1024)
            {
                return $"{size / 1024 / 1024} MB";
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
