using PackageUploader.UI.Providers;
using PackageUploader.UI.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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

        public ICommand CancelCreationCommand { get; }


        public PackagingProgressViewModel(PackingProgressPercentageProvider packingProgressPercentageProvider, PackageUploader.UI.Utility.IWindowService windowService)
        {
            _packingProgressPercentageProvider = packingProgressPercentageProvider;
            _packingProgressPercentageProvider.PropertyChanged += PackagingProgressUpdate;
            _windowService = windowService;

            CancelCreationCommand = new RelayCommand(CancelCreation);
        }

        public void PackagingProgressUpdate(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PackingProgressPercentageProvider.PackingProgressPercentage))
            {
                OnPropertyChanged(nameof(PackingProgressPercentage));
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
