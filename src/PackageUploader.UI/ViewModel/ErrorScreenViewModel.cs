// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using System.Diagnostics;
using System.Windows.Input;

namespace PackageUploader.UI.ViewModel
{
    public partial class ErrorScreenViewModel : BaseViewModel
    {
        private readonly IWindowService _windowService;
        private readonly ErrorModelProvider _errorModelProvider;
        private readonly IClipboardService _clipboardService;
        private readonly IProcessStarterService _processStarterService;

        public string ErrorTitle => _errorModelProvider.Error.MainMessage;
        public string ErrorDescription => _errorModelProvider.Error.DetailMessage;
        public bool HasLogs => !string.IsNullOrWhiteSpace(_errorModelProvider.Error.LogsPath);
        public string RecoveryGuidance
        {
            get
            {
                Type? originPage = _errorModelProvider.Error.OriginPage;
                if (originPage == typeof(PackageCreationView))
                {
                    return Resources.Strings.ErrorPage.PackagingRecoveryGuidance;
                }

                if (originPage == typeof(PackageUploadView) || originPage == typeof(Msixvc2UploadView))
                {
                    return Resources.Strings.ErrorPage.UploadRecoveryGuidance;
                }

                return Resources.Strings.ErrorPage.GenericRecoveryGuidance;
            }
        }

        public ICommand CopyErrorCommand { get; }
        public ICommand GoBackAndFixCommand { get; }
        public ICommand ViewLogsCommand { get; }
        public ICommand HomeCommand { get; }

        public ErrorScreenViewModel(IWindowService windowService, 
                                    ErrorModelProvider errorModelProvider, 
                                    IClipboardService clipboardService,
                                    IProcessStarterService processStarterService)
        {
            _windowService = windowService;
            _errorModelProvider = errorModelProvider;
            _clipboardService = clipboardService;
            _processStarterService = processStarterService;

            CopyErrorCommand = new RelayCommand(CopyError);
            GoBackAndFixCommand = new RelayCommand(GoBackAndFix);
            ViewLogsCommand = new RelayCommand(ViewLogs, () => HasLogs);
            HomeCommand = new RelayCommand(GoHome);
        }

        public void CopyError()
        {
            _clipboardService.SetData(DataFormats.Text, ErrorTitle + Environment.NewLine + ErrorDescription);
        }
        public void GoBackAndFix()
        {
            if (_errorModelProvider.Error.OriginPage != null)
            {
                _windowService.NavigateTo(_errorModelProvider.Error.OriginPage);
            }
            else
            {
                // If the origin page is null, navigate to the main page. This must be the view, not
                // the view model: WindowService.NavigateTo rejects any type that is not a UIElement.
                _windowService.NavigateTo(typeof(MainPageView));
            }
        }

        public void ViewLogs()
        {
            string logPath = _errorModelProvider.Error.LogsPath;
            _processStarterService.Start("explorer.exe", $"/select, \"{logPath}\"");
        }

        public void GoHome()
        {
            _windowService.NavigateTo(typeof(MainPageView));
        }
    }
}
