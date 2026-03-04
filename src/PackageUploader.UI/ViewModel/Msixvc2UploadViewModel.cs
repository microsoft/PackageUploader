// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml;
using Microsoft.Extensions.Logging;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.UI.Model;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;

namespace PackageUploader.UI.ViewModel;

public partial class Msixvc2UploadViewModel : BaseViewModel
{
    private readonly IWindowService _windowService;
    private readonly IPackageUploaderService _uploaderService;
    private readonly PathConfigurationProvider _pathConfigurationProvider;
    private readonly ErrorModelProvider _errorModelProvider;
    private readonly ILogger<Msixvc2UploadViewModel> _logger;

    private Process? _packProcess;
    private Process? _uploadProcess;
    private CancellationTokenSource _cancellationTokenSource = new();

    private GameProduct? _gameProduct = null;
    private IReadOnlyCollection<IGamePackageBranch>? _branchesAndFlights = null;

    // Step 1 — Build location
    private string _contentPath = string.Empty;
    public string ContentPath
    {
        get => _contentPath;
        set
        {
            if (_contentPath != value)
            {
                _contentPath = value;
                OnPropertyChanged(nameof(ContentPath));
                LoadGameConfigValues();
                EstimateFolderSize();
            }
        }
    }

    private string _contentPathError = string.Empty;
    public string ContentPathError
    {
        get => _contentPathError;
        set => SetProperty(ref _contentPathError, value);
    }

    private bool _isAdditionalDetailsExpanded = false;
    public bool IsAdditionalDetailsExpanded
    {
        get => _isAdditionalDetailsExpanded;
        set => SetProperty(ref _isAdditionalDetailsExpanded, value);
    }

    private string _mappingDataXmlPath = string.Empty;
    public string MappingDataXmlPath
    {
        get => _mappingDataXmlPath;
        set
        {
            if (_mappingDataXmlPath != value)
            {
                _mappingDataXmlPath = value;
                OnPropertyChanged(nameof(MappingDataXmlPath));
                EstimateFolderSize();
            }
        }
    }

    private string _subValPath = string.Empty;
    public string SubValPath
    {
        get => _subValPath;
        set => SetProperty(ref _subValPath, value);
    }

    // Step 2 — Destination
    private string _branchOrFlightDisplayName = string.Empty;
    public string BranchOrFlightDisplayName
    {
        get => _branchOrFlightDisplayName;
        set
        {
            if (SetProperty(ref _branchOrFlightDisplayName, value) && value != null)
            {
                UpdateMarketGroups();
            }
        }
    }

    private string[] _branchAndFlightNames = [];
    public string[] BranchAndFlightNames
    {
        get => _branchAndFlightNames;
        set => SetProperty(ref _branchAndFlightNames, value);
    }

    private bool _isLoadingBranchesAndFlights = false;
    public bool IsLoadingBranchesAndFlights
    {
        get => _isLoadingBranchesAndFlights;
        set => SetProperty(ref _isLoadingBranchesAndFlights, value);
    }

    private string _branchOrFlightErrorMessage = string.Empty;
    public string BranchOrFlightErrorMessage
    {
        get => _branchOrFlightErrorMessage;
        set => SetProperty(ref _branchOrFlightErrorMessage, value);
    }

    private string _marketGroupName = string.Empty;
    public string MarketGroupName
    {
        get => _marketGroupName;
        set => SetProperty(ref _marketGroupName, value);
    }

    private string[] _marketGroupNames = [];
    public string[] MarketGroupNames
    {
        get => _marketGroupNames;
        set => SetProperty(ref _marketGroupNames, value);
    }

    private bool _isLoadingMarkets = false;
    public bool IsLoadingMarkets
    {
        get => _isLoadingMarkets;
        set => SetProperty(ref _isLoadingMarkets, value);
    }

    private string _marketGroupErrorMessage = string.Empty;
    public string MarketGroupErrorMessage
    {
        get => _marketGroupErrorMessage;
        set => SetProperty(ref _marketGroupErrorMessage, value);
    }

    // Upload preview
    private string _productName = string.Empty;
    public string ProductName
    {
        get => _productName;
        set => SetProperty(ref _productName, value);
    }

    private string _packageIdentityName = string.Empty;
    public string PackageIdentityName
    {
        get => _packageIdentityName;
        set => SetProperty(ref _packageIdentityName, value);
    }

    private string _estimatedFolderSize = string.Empty;
    public string EstimatedFolderSize
    {
        get => _estimatedFolderSize;
        set => SetProperty(ref _estimatedFolderSize, value);
    }

    private string _bigId = string.Empty;
    public string BigId
    {
        get => _bigId;
        set => SetProperty(ref _bigId, value);
    }

    private string _packageType = string.Empty;
    public string PackageType
    {
        get => _packageType;
        set => SetProperty(ref _packageType, value);
    }

    private BitmapImage? _packagePreviewImage = null;
    public BitmapImage? PackagePreviewImage
    {
        get => _packagePreviewImage;
        set => SetProperty(ref _packagePreviewImage, value);
    }

    // Packaging state
    private bool _isPackaging = false;
    public bool IsPackaging
    {
        get => _isPackaging;
        set => SetProperty(ref _isPackaging, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private int _progressValue = 0;
    public int ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    // Commands
    public ICommand BrowseContentPathCommand { get; }
    public ICommand BrowseMappingDataXmlPathCommand { get; }
    public ICommand BrowseSubValPathCommand { get; }
    public ICommand ContentPathDroppedCommand { get; }
    public ICommand UploadPackageCommand { get; }
    public ICommand CancelButtonCommand { get; }

    public Msixvc2UploadViewModel(IWindowService windowService,
                                  IPackageUploaderService uploaderService,
                                  PathConfigurationProvider pathConfigurationProvider,
                                  ErrorModelProvider errorModelProvider,
                                  ILogger<Msixvc2UploadViewModel> logger)
    {
        _windowService = windowService;
        _uploaderService = uploaderService;
        _pathConfigurationProvider = pathConfigurationProvider;
        _errorModelProvider = errorModelProvider;
        _logger = logger;

        BrowseContentPathCommand = new RelayCommand(OnBrowseContentPath);
        BrowseMappingDataXmlPathCommand = new RelayCommand(OnBrowseMappingDataXml);
        BrowseSubValPathCommand = new RelayCommand(OnBrowseSubValPath);
        ContentPathDroppedCommand = new RelayCommand<string>(path =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                ContentPath = path;
            }
        });
        UploadPackageCommand = new RelayCommand(ExecuteUploadPackageAsync);
        CancelButtonCommand = new RelayCommand(() =>
        {
            if (IsPackaging)
            {
                _cancellationTokenSource.Cancel();
                if (_packProcess != null && !_packProcess.HasExited) _packProcess.Kill();
                if (_uploadProcess != null && !_uploadProcess.HasExited) _uploadProcess.Kill();
                IsPackaging = false;
                StatusMessage = "Cancelled.";
                return;
            }
            _windowService.NavigateTo(typeof(MainPageView));
        });
    }

    private void OnBrowseContentPath()
    {
        var folderDialog = new FolderBrowserDialog();
        if (folderDialog.ShowDialog() == DialogResult.OK)
        {
            ContentPath = folderDialog.SelectedPath;
        }
    }

    private void OnBrowseMappingDataXml()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
            Title = "Select Layout File"
        };

        if (dialog.ShowDialog() == true)
        {
            MappingDataXmlPath = dialog.FileName;
        }
    }

    private void OnBrowseSubValPath()
    {
        var folderDialog = new FolderBrowserDialog();
        if (folderDialog.ShowDialog() == DialogResult.OK)
        {
            SubValPath = folderDialog.SelectedPath;
        }
    }

    private void LoadGameConfigValues()
    {
        ContentPathError = string.Empty;
        ProductName = string.Empty;
        PackageIdentityName = string.Empty;
        BigId = string.Empty;
        PackageType = string.Empty;
        PackagePreviewImage = null;

        if (string.IsNullOrEmpty(ContentPath) || !Directory.Exists(ContentPath))
        {
            return;
        }

        string gameConfigPath = Path.Combine(ContentPath, "MicrosoftGame.config");
        if (!File.Exists(gameConfigPath))
        {
            ContentPathError = Resources.Strings.PackageCreation.FolderDoesNotContainConfigErrMsg;
            return;
        }

        PartialGameConfigModel gameConfig;
        try
        {
            gameConfig = new PartialGameConfigModel(gameConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading game config.");
            ContentPathError = Resources.Strings.PackageCreation.MicrosoftGameConfigInvalidErrMsg;
            return;
        }

        if (string.IsNullOrEmpty(gameConfig.Identity.Name))
        {
            ContentPathError = Resources.Strings.PackageCreation.MsftGameCOnfigLacksValidIdentityErrMsg;
            return;
        }

        PackageIdentityName = gameConfig.Identity.Name;
        ProductName = !string.IsNullOrEmpty(gameConfig.ShellVisuals.DefaultDisplayName)
            ? gameConfig.ShellVisuals.DefaultDisplayName
            : gameConfig.Identity.Name;
        BigId = !string.IsNullOrEmpty(gameConfig.StoreId) ? gameConfig.StoreId : "None";

        if (!string.IsNullOrEmpty(gameConfig.ShellVisuals.Square150x150Logo) &&
            File.Exists(gameConfig.ShellVisuals.Square150x150Logo))
        {
            PackagePreviewImage = LoadBitmapImage(gameConfig.ShellVisuals.Square150x150Logo);
        }

        try
        {
            PackageType = gameConfig.GetDeviceFamily();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device family from game config.");
            ContentPathError = ex.Message;
        }

        // Fetch branches/flights from API if we have a valid StoreId
        if (!string.IsNullOrEmpty(gameConfig.StoreId))
        {
            GetProductInfoAsync();
        }
    }

    private async void GetProductInfoAsync()
    {
        if (string.IsNullOrEmpty(BigId) || BigId == "None")
        {
            return;
        }

        IsLoadingBranchesAndFlights = true;
        BranchOrFlightErrorMessage = string.Empty;

        try
        {
            _gameProduct = await _uploaderService.GetProductByBigIdAsync(BigId, CancellationToken.None);

            if (_gameProduct != null)
            {
                ProductName = _gameProduct.ProductName;
            }

            _branchesAndFlights = await _uploaderService.GetPackageBranchesAsync(_gameProduct, CancellationToken.None);

            List<string> displayNames = [];
            foreach (var branch in _branchesAndFlights)
            {
                if (branch.BranchType == GamePackageBranchType.Branch)
                    displayNames.Add("Branch: " + branch.Name);
                else if (branch.BranchType == GamePackageBranchType.Flight)
                    displayNames.Add("Flight: " + branch.Name);
            }
            BranchAndFlightNames = [.. displayNames];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product info for BigId {BigId}.", BigId);
            BranchOrFlightErrorMessage = $"Error loading branches: {ex.Message}";
        }
        finally
        {
            IsLoadingBranchesAndFlights = false;
        }
    }

    private async void UpdateMarketGroups()
    {
        if (_branchesAndFlights == null)
        {
            return;
        }

        IsLoadingMarkets = true;
        MarketGroupErrorMessage = string.Empty;

        try
        {
            IGamePackageBranch? branchOrFlight = GetBranchOrFlightFromUISelection();

            if (branchOrFlight != null)
            {
                var config = await _uploaderService.GetPackageConfigurationAsync(
                    _gameProduct, branchOrFlight, CancellationToken.None);

                List<string> marketNames = [];
                foreach (var market in config.MarketGroupPackages)
                {
                    marketNames.Add(market.Name);
                }
                MarketGroupNames = [.. marketNames];
                MarketGroupName = MarketGroupNames.FirstOrDefault(string.Empty);
            }
            else
            {
                MarketGroupNames = [];
                MarketGroupName = string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading market groups.");
            MarketGroupErrorMessage = $"Error loading market groups: {ex.Message}";
        }
        finally
        {
            IsLoadingMarkets = false;
        }
    }

    private IGamePackageBranch? GetBranchOrFlightFromUISelection()
    {
        if (_branchesAndFlights == null || string.IsNullOrEmpty(BranchOrFlightDisplayName))
        {
            return null;
        }

        return _branchesAndFlights.FirstOrDefault(x =>
        {
            bool isBranch = BranchOrFlightDisplayName.StartsWith("Branch: ");
            var name = BranchOrFlightDisplayName[(BranchOrFlightDisplayName.IndexOf(':') + 2)..];
            return x.Name == name &&
                   x.BranchType == (isBranch ? GamePackageBranchType.Branch : GamePackageBranchType.Flight);
        });
    }

    private void EstimateFolderSize()
    {
        if (string.IsNullOrEmpty(ContentPath) || !Directory.Exists(ContentPath))
        {
            EstimatedFolderSize = string.Empty;
            return;
        }

        try
        {
            long sizeInBytes;
            if (!string.IsNullOrEmpty(MappingDataXmlPath) && File.Exists(MappingDataXmlPath))
            {
                sizeInBytes = ParseLayoutFileForFileSize(MappingDataXmlPath);
            }
            else
            {
                sizeInBytes = GetDirectorySizeInBytes(ContentPath);
            }

            double sizeInGB = sizeInBytes / (1024.0 * 1024.0 * 1024.0);
            EstimatedFolderSize = sizeInGB < 1 ? "< 1 GB" : $"{sizeInGB:F2} GB";
        }
        catch (Exception ex)
        {
            EstimatedFolderSize = "Unknown";
            _logger.LogError(ex, "Error calculating folder size.");
        }
    }

    private static long GetDirectorySizeInBytes(string path)
    {
        long size = 0;
        DirectoryInfo dirInfo = new(path);
        FileInfo[] files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
        foreach (FileInfo file in files)
        {
            size += file.Length;
        }
        return size;
    }

    private long ParseLayoutFileForFileSize(string mappingDataXmlPath)
    {
        try
        {
            XmlDocument xmlDoc = new();
            xmlDoc.Load(mappingDataXmlPath);

            XmlNodeList? fileGroups = xmlDoc.SelectNodes("//FileGroup");
            if (fileGroups == null || fileGroups.Count == 0)
            {
                return 0;
            }

            long totalSize = 0;
            HashSet<string> processedFiles = new(StringComparer.OrdinalIgnoreCase);

            foreach (XmlNode fileGroup in fileGroups)
            {
                string? sourcePath = fileGroup.Attributes?["SourcePath"]?.Value;
                string? include = fileGroup.Attributes?["Include"]?.Value;

                if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(include))
                {
                    continue;
                }

                string fullPath = Path.Combine(sourcePath, include);
                if (!processedFiles.Add(fullPath))
                {
                    continue;
                }

                if (File.Exists(fullPath))
                {
                    totalSize += new FileInfo(fullPath).Length;
                }
            }

            return totalSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing layout file for size.");
            return GetDirectorySizeInBytes(ContentPath);
        }
    }

    private static BitmapImage LoadBitmapImage(string imagePath)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(imagePath);
        image.EndInit();
        image.Freeze();
        return image;
    }

    // ── Upload Package flow ────────────────────────────────────────────

    private async void ExecuteUploadPackageAsync()
    {
        if (IsPackaging)
        {
            return;
        }

        // Validation
        string makePkg2Path = _pathConfigurationProvider.MakePkg2Path;
        if (string.IsNullOrEmpty(makePkg2Path) || !File.Exists(makePkg2Path))
        {
            ContentPathError = "makepkg2 was not found. Please install the microsoft.xbox.packaging.tools.makepkg2 NuGet package.";
            return;
        }

        if (string.IsNullOrEmpty(ContentPath) || !Directory.Exists(ContentPath))
        {
            ContentPathError = "Please provide a valid content path.";
            return;
        }

        string gameConfigPath = Path.Combine(ContentPath, "MicrosoftGame.config");
        if (!File.Exists(gameConfigPath))
        {
            ContentPathError = Resources.Strings.PackageCreation.FolderDoesNotContainConfigErrMsg;
            return;
        }

        if (string.IsNullOrEmpty(BranchOrFlightDisplayName))
        {
            BranchOrFlightErrorMessage = "Please select a destination branch or flight.";
            return;
        }

        if (string.IsNullOrEmpty(MarketGroupName))
        {
            MarketGroupErrorMessage = "Please select a market group.";
            return;
        }

        // Prepare output directory
        string outputDir = Path.Combine(Path.GetTempPath(), "MSIXVC2_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(outputDir);

        _cancellationTokenSource = new CancellationTokenSource();
        IsPackaging = true;
        StatusMessage = "Creating MSIXVC2 package...";
        ProgressValue = 0;

        // Auto-generate layout file if not provided
        if (string.IsNullOrEmpty(MappingDataXmlPath) || !File.Exists(MappingDataXmlPath))
        {
            StatusMessage = "Generating layout file...";
            bool genMapSuccess = await RunMakePkg2GenMap(makePkg2Path, outputDir);
            if (!genMapSuccess)
            {
                IsPackaging = false;
                return;
            }
        }

        // Run makepkg2 pack
        StatusMessage = "Packaging with makepkg2...";
        bool packSuccess = await RunMakePkg2Pack(makePkg2Path, outputDir);

        if (!packSuccess)
        {
            IsPackaging = false;
            return;
        }

        // Run makepkg2 upload
        StatusMessage = "Uploading package to Partner Center...";
        bool uploadSuccess = await RunMakePkg2Upload(makePkg2Path, outputDir);

        IsPackaging = false;

        if (uploadSuccess)
        {
            StatusMessage = "Upload complete.";
            _logger.LogInformation("MSIXVC2 package uploaded successfully.");
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.NavigateTo(typeof(UploadingFinishedView));
            });
        }
    }

    private async Task<bool> RunMakePkg2GenMap(string makePkg2Path, string outputDir)
    {
        string layoutFile = Path.Combine(outputDir, "generated_layout.xml");
        // makepkg2 genmap requires the output file to already exist
        File.Create(layoutFile).Dispose();
        string arguments = $"genmap /f \"{layoutFile}\" /d \"{ContentPath}\"";

        ArrayList processOutput = [];
        ArrayList processErrorOutput = [];

        var process = new Process();
        process.StartInfo.FileName = makePkg2Path;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.EnableRaisingEvents = true;
        process.StartInfo.CreateNoWindow = true;

        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data)) processOutput.Add(args.Data);
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data)) processErrorOutput.Add(args.Data);
        };

        _logger.LogInformation("Running: {Command} {Arguments}", makePkg2Path, arguments);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            string errorString = string.Join("\n", processErrorOutput.ToArray());
            _logger.LogError("makepkg2 genmap failed with exit code {ExitCode}: {Error}", process.ExitCode, errorString);
            ContentPathError = "Failed to generate layout file.";
            return false;
        }

        if (File.Exists(layoutFile))
        {
            MappingDataXmlPath = layoutFile;
        }

        return File.Exists(MappingDataXmlPath);
    }

    private async Task<bool> RunMakePkg2Pack(string makePkg2Path, string outputDir)
    {
        string arguments = $"pack /msixvc2 /d \"{ContentPath}\" /pd \"{outputDir}\" /pc /v";

        if (!string.IsNullOrEmpty(MappingDataXmlPath) && File.Exists(MappingDataXmlPath))
        {
            arguments += $" /f \"{MappingDataXmlPath}\"";
        }

        if (!string.IsNullOrEmpty(SubValPath) && Directory.Exists(SubValPath))
        {
            arguments += $" /validationpath \"{SubValPath}\"";
        }

        ArrayList processOutput = [];
        ArrayList processErrorOutput = [];

        _packProcess = new Process();
        _packProcess.StartInfo.FileName = makePkg2Path;
        _packProcess.StartInfo.Arguments = arguments;
        _packProcess.StartInfo.RedirectStandardOutput = true;
        _packProcess.StartInfo.RedirectStandardError = true;
        _packProcess.EnableRaisingEvents = true;
        _packProcess.StartInfo.CreateNoWindow = true;

        _packProcess.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);

                // Parse encryption progress: "Encrypted XX %"
                if (args.Data.Contains("Encrypted") && args.Data.Contains("%"))
                {
                    var parts = args.Data.Split(' ');
                    foreach (var part in parts)
                    {
                        if (int.TryParse(part, out int pct))
                        {
                            // Map 0-100 to 5-50 (pack is first half, upload is second)
                            ProgressValue = (int)(pct * 0.45 + 5);
                            break;
                        }
                    }
                }
            }
        };

        _packProcess.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                processErrorOutput.Add(args.Data);
            }
        };

        _logger.LogInformation("Running: {Command} {Arguments}", makePkg2Path, arguments);
        _packProcess.Start();
        _packProcess.BeginOutputReadLine();
        _packProcess.BeginErrorReadLine();
        await _packProcess.WaitForExitAsync();

        // Log output to file
        string outputString = string.Join("\n", processOutput.ToArray());
        outputString += "\n" + string.Join("\n", processErrorOutput.ToArray());
        string logFilePath = Path.Combine(Path.GetTempPath(), $"PackageUploader_UI_MakePkg2_Pack_{DateTime.Now:yyyyMMddHHmmss}.log");
        File.WriteAllText(logFilePath, outputString);

        _logger.LogInformation("makepkg2 pack log written to {LogFilePath}.", logFilePath);

        if (_packProcess.ExitCode != 0)
        {
            string errorString = string.Join("\n", processErrorOutput.ToArray());

            _errorModelProvider.Error.MainMessage = "Error creating MSIXVC2 package.";
            _errorModelProvider.Error.DetailMessage = errorString;
            _errorModelProvider.Error.OriginPage = typeof(Msixvc2UploadView);
            _errorModelProvider.Error.LogsPath = logFilePath;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.NavigateTo(typeof(ErrorPageView));
            });

            _logger.LogError("makepkg2 pack failed with exit code {ExitCode}.", _packProcess.ExitCode);
            return false;
        }

        ProgressValue = 50;
        _logger.LogInformation("makepkg2 pack completed successfully.");

        // Ensure file handles are fully released before upload
        _packProcess.Dispose();
        _packProcess = null;

        return true;
    }

    private async Task<bool> RunMakePkg2Upload(string makePkg2Path, string outputDir)
    {
        // Verify the .msixvc file exists before attempting upload
        var msixvcFiles = Directory.GetFiles(outputDir, "*.msixvc");
        if (msixvcFiles.Length == 0)
        {
            _logger.LogError("No .msixvc file found in {OutputDir} after pack.", outputDir);
            ContentPathError = "Package file not found after pack completed.";
            return false;
        }
        _logger.LogInformation("Found package: {PackageFile}", msixvcFiles[0]);

        // Parse branch or flight from UI selection
        bool isBranch = BranchOrFlightDisplayName.StartsWith("Branch: ");
        string name = BranchOrFlightDisplayName[(BranchOrFlightDisplayName.IndexOf(':') + 2)..];

        // Use the output directory path — makepkg2 upload /pd expects the directory containing the .msixvc and chunk blobs
        string arguments = $"upload /pd \"{outputDir}\" /auth Browser /v";

        if (isBranch)
        {
            arguments += $" /branch \"{name}\"";
        }
        else
        {
            arguments += $" /flight \"{name}\"";
        }

        if (!string.IsNullOrEmpty(MarketGroupName))
        {
            arguments += $" /market \"{MarketGroupName}\"";
        }

        ArrayList processOutput = [];
        ArrayList processErrorOutput = [];

        _uploadProcess = new Process();
        _uploadProcess.StartInfo.FileName = makePkg2Path;
        _uploadProcess.StartInfo.Arguments = arguments;
        _uploadProcess.StartInfo.RedirectStandardOutput = true;
        _uploadProcess.StartInfo.RedirectStandardError = true;
        _uploadProcess.EnableRaisingEvents = true;
        _uploadProcess.StartInfo.CreateNoWindow = true;

        _uploadProcess.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);

                // Parse upload progress (map 50-100%)
                if (args.Data.Contains("Uploading") && args.Data.Contains("%"))
                {
                    var parts = args.Data.Split(' ');
                    foreach (var part in parts)
                    {
                        string trimmed = part.TrimEnd('%');
                        if (int.TryParse(trimmed, out int pct))
                        {
                            ProgressValue = (int)(pct * 0.5 + 50);
                            break;
                        }
                    }
                }
            }
        };

        _uploadProcess.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                processErrorOutput.Add(args.Data);
            }
        };

        _logger.LogInformation("Running: {Command} {Arguments}", makePkg2Path, arguments);
        _uploadProcess.Start();
        _uploadProcess.BeginOutputReadLine();
        _uploadProcess.BeginErrorReadLine();
        await _uploadProcess.WaitForExitAsync();

        // Log output to file
        string outputString = string.Join("\n", processOutput.ToArray());
        outputString += "\n" + string.Join("\n", processErrorOutput.ToArray());
        string logFilePath = Path.Combine(Path.GetTempPath(), $"PackageUploader_UI_MakePkg2_Upload_{DateTime.Now:yyyyMMddHHmmss}.log");
        File.WriteAllText(logFilePath, outputString);

        _logger.LogInformation("makepkg2 upload log written to {LogFilePath}.", logFilePath);

        if (_uploadProcess.ExitCode != 0)
        {
            string errorString = string.Join("\n", processErrorOutput.ToArray());

            _errorModelProvider.Error.MainMessage = "Error uploading MSIXVC2 package.";
            _errorModelProvider.Error.DetailMessage = errorString;
            _errorModelProvider.Error.OriginPage = typeof(Msixvc2UploadView);
            _errorModelProvider.Error.LogsPath = logFilePath;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.NavigateTo(typeof(ErrorPageView));
            });

            _logger.LogError("makepkg2 upload failed with exit code {ExitCode}.", _uploadProcess.ExitCode);
            return false;
        }

        ProgressValue = 100;
        _logger.LogInformation("makepkg2 upload completed successfully.");
        return true;
    }
}
