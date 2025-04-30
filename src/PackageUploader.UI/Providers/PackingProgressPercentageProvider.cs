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
        private int _packingProgressPercentage;
        public int PackingProgressPercentage
        {
            get => _packingProgressPercentage;
            set
            {
                _packingProgressPercentage = value;
                OnPropertyChanged(nameof(PackingProgressPercentage));
            }
        }

        private bool _packingCancelled;
        public bool PackingCancelled
        {
            get => _packingCancelled;
            set
            {
                _packingCancelled = value;
                OnPropertyChanged(nameof(PackingCancelled));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
