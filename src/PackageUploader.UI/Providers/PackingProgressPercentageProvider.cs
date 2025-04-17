using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace PackageUploader.UI.Providers
{
    public partial class PackingProgressPercentageProvider : INotifyPropertyChanged
    {
        private double _packingProgressPercentage;
        public double PackingProgressPercentage
        {
            get => _packingProgressPercentage;
            set
            {
                _packingProgressPercentage = value;
                OnPropertyChanged();
            }
        }

        private bool _packingCancelled;
        public bool PackingCancelled
        {
            get => _packingCancelled;
            set
            {
                _packingCancelled = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
