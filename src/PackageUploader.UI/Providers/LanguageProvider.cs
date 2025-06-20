// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PackageUploader.UI.Providers
{
    public class LanguageProvider : INotifyPropertyChanged
    {
        private CultureInfo _currentCulture;

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value;
                    ApplyCulture(value);
                    OnPropertyChanged();
                }
            }
        }

        // Available cultures in the application
        public List<CultureInfo> AvailableCultures { get; } =
        [
            new("en"), // Default English
            new("ja-JP"), // Japanese
            new("ko-KR"), // Korean
            new("zh-CN"), // Simplified Chinese
        ];

        public LanguageProvider()
        {
            // Initialize with current system culture or default to English
            _currentCulture = CultureInfo.CurrentUICulture;
            
            // If the current culture isn't in our list, default to English
            if (!AvailableCultures.Any(c => c.Name == _currentCulture.Name))
            {
                _currentCulture = new CultureInfo("en");
            }

            // Apply the selected culture on initialization
            ApplyCulture(_currentCulture);
        }

        private static void ApplyCulture(CultureInfo cultureInfo)
        {
            // Set the current thread culture
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;

            // Force refresh resource dictionaries
            foreach (var dict in System.Windows.Application.Current.Resources.MergedDictionaries)
            {
                dict?.MergedDictionaries.Clear();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}