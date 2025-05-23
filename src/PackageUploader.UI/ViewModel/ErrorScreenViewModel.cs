// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using System.Diagnostics;
using System.Windows.Input;

namespace PackageUploader.UI.ViewModel
{
    public partial class ErrorScreenViewModel : BaseViewModel
    {
        private readonly IWindowService _windowService;
        private readonly ErrorModelProvider _errorModelProvider;

        public string ErrorTitle => _errorModelProvider.Error.MainMessage;
        public string ErrorDescription => _errorModelProvider.Error.DetailMessage;

        public ICommand CopyErrorCommand { get; }
        public ICommand GoBackAndFixCommand { get; }
        public ICommand ViewLogsCommand { get; }

        public ErrorScreenViewModel(IWindowService windowService, ErrorModelProvider errorModelProvider)
        {
            _windowService = windowService;
            _errorModelProvider = errorModelProvider;

            CopyErrorCommand = new RelayCommand(CopyError);
            GoBackAndFixCommand = new RelayCommand(GoBackAndFix);
            ViewLogsCommand = new RelayCommand(ViewLogs);
        }

        public void CopyError()
        {
            Clipboard.SetData(DataFormats.Text, ErrorTitle + Environment.NewLine + ErrorDescription);
        }
        public void GoBackAndFix()
        {
            //System.Windows.Application.Current.Dispatcher.Invoke(() =>
            //{
                _windowService.NavigateTo(_errorModelProvider.Error.OriginPage);
            //});
        }

        public void ViewLogs()
        {
            string logPath = _errorModelProvider.Error.LogsPath;
            Process.Start("explorer.exe", $"/select, \"{logPath}\"");
        }
    }
}
