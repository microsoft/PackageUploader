// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
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

        private bool _isMsixvc2;
        public bool IsMsixvc2
        {
            get => _isMsixvc2;
            set
            {
                _isMsixvc2 = value;
                OnPropertyChanged(nameof(IsMsixvc2));
            }
        }

        public PackingProgressPercentageProvider()
        {
            _packingProgressPercentage = 0;
            _packingCancelled = false;
            _isMsixvc2 = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
