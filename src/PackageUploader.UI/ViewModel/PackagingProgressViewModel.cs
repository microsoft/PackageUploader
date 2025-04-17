using PackageUploader.UI.Providers;
using PackageUploader.UI.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace PackageUploader.UI.ViewModel
{
    public partial class PackagingProgressViewModel : BaseViewModel
    {
        public readonly PackingProgressPercentageProvider _packingProgressPercentageProvider;
        private readonly PackageUploader.UI.Utility.IWindowService _windowService;

        public double PackingProgressPercentage
        {
            get => _packingProgressPercentageProvider.PackingProgressPercentage;
            set
            {
                if (_packingProgressPercentageProvider.PackingProgressPercentage != value)
                {
                    _packingProgressPercentageProvider.PackingProgressPercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        private ImageSource _validatingFilesImage = null;
        public ImageSource ValidatingFilesImage
        {
            get => _validatingFilesImage;
            set => SetProperty(ref _validatingFilesImage, value);
        }
        private ImageSource _copyingAndEncryptingDataImage = null;
        public ImageSource CopyingAndEncryptingDataImage
        {
            get => _copyingAndEncryptingDataImage;
            set => SetProperty(ref _copyingAndEncryptingDataImage, value);
        }
        public ImageSource _verifyingPackageContentsImage = null;
        public ImageSource VerifyingPackageContentsImage
        {
            get => _verifyingPackageContentsImage;
            set => SetProperty(ref _verifyingPackageContentsImage, value);
        }

        public ICommand CancelCreationCommand { get; }


        public PackagingProgressViewModel(PackingProgressPercentageProvider packingProgressPercentageProvider, PackageUploader.UI.Utility.IWindowService windowService)
        {
            _packingProgressPercentageProvider = packingProgressPercentageProvider;
            _packingProgressPercentageProvider.PropertyChanged += PackagingProgressUpdate;
            _windowService = windowService;

            CancelCreationCommand = new RelayCommand(CancelCreation);

            ValidatingFilesImage = new ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/AppIcon/Settings.png") as ImageSource; // thanks copilot
        }

        public void PackagingProgressUpdate(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PackingProgressPercentageProvider.PackingProgressPercentage))
            {
                OnPropertyChanged(nameof(PackingProgressPercentage));

                if(PackingProgressPercentage > 0)
                {
                    ValidatingFilesImage = new ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/AppIcon/Accept.png") as ImageSource;
                    CopyingAndEncryptingDataImage = new ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/AppIcon/Settings.png") as ImageSource;
                }
                else if(PackingProgressPercentage >= 95)
                {
                    CopyingAndEncryptingDataImage = new ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/AppIcon/Accept.png") as ImageSource;
                    VerifyingPackageContentsImage = new ImageSourceConverter().ConvertFromString("pack://application:,,,/Resources/AppIcon/Settings.png") as ImageSource;
                }
            }
        }

        private void CancelCreation()
        {
            _packingProgressPercentageProvider.PackingCancelled = true;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.NavigateTo(typeof(PackageCreationView2));
            });
        }
    }
}
