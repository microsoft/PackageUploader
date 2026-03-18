// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;

namespace PackageUploader.UI.ViewModel;

public partial class Msixvc2UploadingViewModel : BaseViewModel
{
    private readonly IWindowService _windowService;
    private readonly ILogger<Msixvc2UploadingViewModel> _logger;
    private readonly ErrorModelProvider _errorModelProvider;
    private readonly PackageModelProvider _packageModelProvider;

    private Process? _makePkg2Process;
    private string _operationLogOutput = string.Empty;
    private string _lastLogFilePath = string.Empty;
    private bool _isCancelled;

    private int _uploadPercentage;
    public int UploadPercentage
    {
        get => _uploadPercentage;
        set => SetProperty(ref _uploadPercentage, value);
    }

    private Msixvc2UploadStage _uploadStage = Msixvc2UploadStage.NotStarted;
    public Msixvc2UploadStage UploadStage
    {
        get => _uploadStage;
        set => SetProperty(ref _uploadStage, value);
    }

    private string _uploadStats = string.Empty;
    public string UploadStats
    {
        get => _uploadStats;
        set => SetProperty(ref _uploadStats, value);
    }

    private bool _showUploadStats;
    public bool ShowUploadStats
    {
        get => _showUploadStats;
        set => SetProperty(ref _showUploadStats, value);
    }

    public ICommand CancelUploadCommand { get; }

    public Msixvc2UploadingViewModel(
        IWindowService windowService,
        ILogger<Msixvc2UploadingViewModel> logger,
        ErrorModelProvider errorModelProvider,
        PackageModelProvider packageModelProvider)
    {
        _windowService = windowService;
        _logger = logger;
        _errorModelProvider = errorModelProvider;
        _packageModelProvider = packageModelProvider;

        CancelUploadCommand = new RelayCommand(CancelUpload);
    }

    public void OnAppearing()
    {
        _isCancelled = false;
        UploadPercentage = 0;
        UploadStage = Msixvc2UploadStage.Preparing;
        UploadStats = string.Empty;
        ShowUploadStats = false;

        StartUploadAsync();
    }

    private async void StartUploadAsync()
    {
        var package = _packageModelProvider.Package;
        string uploadArgs = package.UploadArguments;
        string makePkg2Path = package.MakePkg2Path;

        if (string.IsNullOrEmpty(uploadArgs) || string.IsNullOrEmpty(makePkg2Path))
        {
            SetErrorAndGoToErrorPage("Upload Error",
                "Upload arguments or makepkg2 path not set. Please try again.");
            return;
        }

        _operationLogOutput = string.Empty;
        int lastReportedPct = -1;
        string? lastErrorMessage = null;
        string lastWrittenTotal = string.Empty;

        try
        {
            _logger.LogInformation("Starting makepkg2 loose upload: {Arguments}", uploadArgs);

            int exitCode = await RunMakePkg2ProcessAsync(makePkg2Path, uploadArgs, "Upload", line =>
            {
                if (UploadStage == Msixvc2UploadStage.Preparing)
                {
                    var progressMatch = Regex.Match(line,
                        @"([\d.]+ \S+) / ([\d.]+ \S+) read \(([\d.]+)%\).*?([\d.]+ \S+) / ([\d.]+ \S+) written \(([\d.]+)%\)");
                    if (progressMatch.Success)
                    {
                        UpdateStageOnUIThread(Msixvc2UploadStage.Uploading);
                    }
                }

                var match = Regex.Match(line,
                    @"([\d.]+ \S+) / ([\d.]+ \S+) read \(([\d.]+)%\).*?([\d.]+ \S+) / ([\d.]+ \S+) written \(([\d.]+)%\)");
                if (match.Success)
                {
                    string writtenAmount = match.Groups[4].Value;
                    string writtenTotal = match.Groups[5].Value;
                    string writtenPct = match.Groups[6].Value;
                    string writtenSpeed = string.Empty;

                    var speedMatch = Regex.Match(line, @"written \([\d.]+%\) at ([\d.]+ \S+/s)");
                    if (speedMatch.Success)
                    {
                        writtenSpeed = speedMatch.Groups[1].Value;
                    }

                    if (double.TryParse(writtenPct, out double wp))
                    {
                        int wholePct = (int)wp;
                        if (wholePct != lastReportedPct)
                        {
                            lastReportedPct = wholePct;
                            // Scale upload percentage to 0-80 range, reserving 80-100 for validating+finalizing
                            int scaledPct = (int)(wp * 0.8);
                            UpdatePercentageOnUIThread(scaledPct);

                            string stats = !string.IsNullOrEmpty(writtenSpeed)
                                ? $"{writtenAmount} / {writtenTotal} written at {writtenSpeed}"
                                : $"{writtenAmount} / {writtenTotal} written";
                            UpdateStatsOnUIThread(stats);
                        }
                        lastWrittenTotal = writtenTotal;
                    }
                }

                if (line.Contains("NativeSubmissionValidator", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("validating package", StringComparison.OrdinalIgnoreCase))
                {
                    if (UploadStage < Msixvc2UploadStage.Validating)
                    {
                        UpdateStageOnUIThread(Msixvc2UploadStage.Validating);
                        UpdatePercentageOnUIThread(82);
                    }
                }

                if (line.Contains("Completed validating", StringComparison.OrdinalIgnoreCase))
                {
                    if (UploadStage < Msixvc2UploadStage.Finalizing)
                    {
                        UpdateStageOnUIThread(Msixvc2UploadStage.Finalizing);
                        UpdateFinalizingProgress(0, "Uploading validation log...");
                    }
                }

                if (line.Contains("Ingest package", StringComparison.OrdinalIgnoreCase) &&
                    !line.Contains("Ingested package", StringComparison.OrdinalIgnoreCase))
                {
                    if (UploadStage == Msixvc2UploadStage.Finalizing)
                    {
                        UpdateFinalizingProgress(1, "Ingesting package...");
                    }
                }

                if (line.Contains("Ingested package", StringComparison.OrdinalIgnoreCase))
                {
                    if (UploadStage == Msixvc2UploadStage.Finalizing)
                    {
                        UpdateFinalizingProgress(2, "Committing package...");
                    }
                }

                if (line.Contains("Successfully created package", StringComparison.OrdinalIgnoreCase))
                {
                    if (UploadStage == Msixvc2UploadStage.Finalizing)
                    {
                        UpdateFinalizingProgress(3, "Upload complete!");
                    }
                }

                if (line.Contains("fail:", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Exception:", StringComparison.OrdinalIgnoreCase))
                {
                    lastErrorMessage = line;
                }
            });

            if (_isCancelled) return;

            if (exitCode != 0)
            {
                _logger.LogError("makepkg2 upload failed with exit code {ExitCode}.", exitCode);
                string errorDetail = !string.IsNullOrEmpty(lastErrorMessage)
                    ? lastErrorMessage
                    : $"makepkg2 upload exited with code {exitCode}.";
                SetErrorAndGoToErrorPage("Upload Failed", errorDetail);
                return;
            }

            UpdateStageOnUIThread(Msixvc2UploadStage.Done);
            UpdatePercentageOnUIThread(100);
            _packageModelProvider.Package.UploadSize = lastWrittenTotal;
            _logger.LogInformation("MSIXVC2 loose upload completed successfully.");

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.NavigateTo(typeof(UploadingFinishedView));
            });
        }
        catch (Exception ex)
        {
            if (_isCancelled) return;

            _logger.LogError(ex, "Unexpected error during MSIXVC2 upload.");
            SetErrorAndGoToErrorPage("Unexpected Error", ex.ToString());
        }
    }

    private Task<int> RunMakePkg2ProcessAsync(string exePath, string arguments, string operationName, Action<string>? onOutputLine = null)
    {
        var tcs = new TaskCompletionSource<int>();
        var processOutput = new List<string>();
        var processErrors = new List<string>();

        _makePkg2Process = new Process();
        _makePkg2Process.StartInfo.FileName = exePath;
        _makePkg2Process.StartInfo.Arguments = arguments;
        _makePkg2Process.StartInfo.RedirectStandardOutput = true;
        _makePkg2Process.StartInfo.RedirectStandardError = true;
        _makePkg2Process.StartInfo.CreateNoWindow = true;
        _makePkg2Process.EnableRaisingEvents = true;

        _makePkg2Process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                processOutput.Add(e.Data);
                _logger.LogTrace("[{Op}] {Data}", operationName, e.Data);
                onOutputLine?.Invoke(e.Data);
            }
        };

        _makePkg2Process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                processErrors.Add(e.Data);
                _logger.LogWarning("[{Op}] stderr: {Data}", operationName, e.Data);
            }
        };

        _makePkg2Process.Exited += (s, e) =>
        {
            _operationLogOutput = string.Join("\n", processOutput) + "\n" + string.Join("\n", processErrors);
            WriteLogFile(operationName, _operationLogOutput);

            if (!_makePkg2Process.HasExited)
            {
                _makePkg2Process.WaitForExit();
            }
            tcs.TrySetResult(_makePkg2Process.ExitCode);
        };

        _makePkg2Process.Start();
        _makePkg2Process.BeginOutputReadLine();
        _makePkg2Process.BeginErrorReadLine();

        return tcs.Task;
    }

    private void CancelUpload()
    {
        _isCancelled = true;

        if (_makePkg2Process != null && !_makePkg2Process.HasExited)
        {
            _makePkg2Process.Kill(true);
        }

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _windowService.NavigateTo(typeof(Msixvc2UploadView));
        });
    }

    private void UpdatePercentageOnUIThread(int pct)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            UploadPercentage = pct;
        });
    }

    private void UpdateStageOnUIThread(Msixvc2UploadStage stage)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            UploadStage = stage;
            ShowUploadStats = stage == Msixvc2UploadStage.Uploading || stage == Msixvc2UploadStage.Finalizing;
            if (stage != Msixvc2UploadStage.Uploading && stage != Msixvc2UploadStage.Finalizing)
            {
                UploadStats = string.Empty;
            }
        });
    }

    private void UpdateStatsOnUIThread(string stats)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            UploadStats = stats;
            ShowUploadStats = true;
        });
    }

    // Finalizing has 4 sub-steps (0-3), mapped to percentage increments within the finalizing range
    private void UpdateFinalizingProgress(int subStep, string statusText)
    {
        // Map sub-steps 0-3 to percentage 85-100
        int pct = 85 + (subStep * 5);
        if (pct > 100) pct = 100;

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            UploadPercentage = pct;
            UploadStats = statusText;
            ShowUploadStats = true;
        });
    }

    private void SetErrorAndGoToErrorPage(string title, string detail)
    {
        _errorModelProvider.Error.MainMessage = title;
        _errorModelProvider.Error.DetailMessage = detail;
        _errorModelProvider.Error.OriginPage = typeof(Msixvc2UploadView);
        _errorModelProvider.Error.LogsPath = _lastLogFilePath;

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _windowService.NavigateTo(typeof(ErrorPageView));
        });
    }

    private void WriteLogFile(string operationName, string content)
    {
        _lastLogFilePath = Path.Combine(Path.GetTempPath(),
            $"PackageUploader_UI_Msixvc2_{operationName}_{DateTime.Now:yyyyMMddHHmmss}.log");
        File.WriteAllText(_lastLogFilePath, content);
    }
}
