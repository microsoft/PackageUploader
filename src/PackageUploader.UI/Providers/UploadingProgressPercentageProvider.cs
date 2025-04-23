using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PackageUploader.UI.Providers
{
    public partial class UploadingProgressPercentageProvider : INotifyPropertyChanged
    {
        private int _uploadingProgresPercentage;
        public int UploadingProgressPercentage
        {
            get => _uploadingProgresPercentage;
            set
            {
                _uploadingProgresPercentage = value;
                OnPropertyChanged();
            }
        }

        private bool _uploadingCancelled;
        public bool UploadingCancelled
        {
            get => _uploadingCancelled;
            set
            {
                _uploadingCancelled = value;
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
