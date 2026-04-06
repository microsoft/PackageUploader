// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    private readonly ILogger<Msixvc2UploadViewModel> _logger;
    private readonly ErrorModelProvider _errorModelProvider;
    private readonly PathConfigurationProvider _pathConfigurationService;
    private readonly PackageModelProvider _packageModelProvider;

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
                CheckCanExecuteUploadCommand();
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
                CheckCanExecuteUploadCommand();
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
        set
        {
            if (SetProperty(ref _isLoadingBranchesAndFlights, value))
            {
                CheckCanExecuteUploadCommand();
            }
        }
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
        set
        {
            if (SetProperty(ref _marketGroupName, value))
            {
                CheckCanExecuteUploadCommand();
            }
        }
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
        set
        {
            if (SetProperty(ref _isLoadingMarkets, value))
            {
                CheckCanExecuteUploadCommand();
            }
        }
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

    // Commands
    public ICommand BrowseContentPathCommand { get; }
    public ICommand BrowseMappingDataXmlPathCommand { get; }
    public ICommand ContentPathDroppedCommand { get; }
    public ICommand UploadPackageCommand { get; }
    public ICommand CancelButtonCommand { get; }

    public Msixvc2UploadViewModel(IWindowService windowService,
                                  IPackageUploaderService uploaderService,
                                  ILogger<Msixvc2UploadViewModel> logger,
                                  ErrorModelProvider errorModelProvider,
                                  PathConfigurationProvider pathConfigurationService,
                                  PackageModelProvider packageModelProvider)
    {
        _windowService = windowService;
        _uploaderService = uploaderService;
        _logger = logger;
        _errorModelProvider = errorModelProvider;
        _pathConfigurationService = pathConfigurationService;
        _packageModelProvider = packageModelProvider;

        BrowseContentPathCommand = new RelayCommand(OnBrowseContentPath);
        BrowseMappingDataXmlPathCommand = new RelayCommand(OnBrowseMappingDataXml);
        ContentPathDroppedCommand= new RelayCommand<string>(path =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                ContentPath = path;
            }
        });
        UploadPackageCommand = new RelayCommand(StartPackAndUploadAsync, CanUpload);
        CancelButtonCommand = new RelayCommand(() =>
        {
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

            string mainBranch = displayNames.FirstOrDefault(n => n.Equals("Branch: Main", StringComparison.OrdinalIgnoreCase), string.Empty);
            BranchOrFlightDisplayName = !string.IsNullOrEmpty(mainBranch) ? mainBranch : BranchAndFlightNames.FirstOrDefault(string.Empty);
            OnPropertyChanged(nameof(BranchOrFlightDisplayName));
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

    public async void RefreshBranchesAsync()
    {
        if (_gameProduct == null || string.IsNullOrEmpty(BigId) || BigId == "None")
        {
            return;
        }

        string previousSelection = BranchOrFlightDisplayName;

        IsLoadingBranchesAndFlights = true;
        BranchOrFlightErrorMessage = string.Empty;

        try
        {
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

            if (!string.IsNullOrEmpty(previousSelection) && displayNames.Contains(previousSelection))
            {
                BranchOrFlightDisplayName = previousSelection;
            }
            else
            {
                string mainBranch = displayNames.FirstOrDefault(n => n.Equals("Branch: Main", StringComparison.OrdinalIgnoreCase), string.Empty);
                BranchOrFlightDisplayName = !string.IsNullOrEmpty(mainBranch) ? mainBranch : BranchAndFlightNames.FirstOrDefault(string.Empty);
            }
            OnPropertyChanged(nameof(BranchOrFlightDisplayName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing branches for BigId {BigId}.", BigId);
            BranchOrFlightErrorMessage = $"Error refreshing branches: {ex.Message}";
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

    private bool CanUpload()
    {
        return !string.IsNullOrEmpty(ContentPath)
            && Directory.Exists(ContentPath)
            && string.IsNullOrEmpty(ContentPathError)
            && _gameProduct != null
            && !string.IsNullOrEmpty(BranchOrFlightDisplayName)
            && !string.IsNullOrEmpty(MarketGroupName)
            && !IsLoadingBranchesAndFlights
            && !IsLoadingMarkets;
    }

    private void CheckCanExecuteUploadCommand()
    {
        if (UploadPackageCommand is RelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    private void StartPackAndUploadAsync()
    {
        string makePkg2Path = _pathConfigurationService.MakePkg2Path;
        if (string.IsNullOrEmpty(makePkg2Path) || !File.Exists(makePkg2Path))
        {
            SetErrorAndGoToErrorPage("makepkg2 Not Found",
                "makepkg2.exe was not found. Please install the Microsoft.Xbox.Packaging.Tools.makepkg2 NuGet package.");
            return;
        }

        string uploadArgs = BuildUploadArguments();
        _logger.LogInformation("Navigating to upload progress page with args: {Arguments}", uploadArgs);

        var branchOrFlight = GetBranchOrFlightFromUISelection();
        _packageModelProvider.Package.BigId = BigId;
        _packageModelProvider.Package.PackageType = "PC";
        _packageModelProvider.Package.PackagePreviewImage = PackagePreviewImage;
        _packageModelProvider.Package.PackageName = ProductName;
        _packageModelProvider.Package.Destination = BranchOrFlightDisplayName;
        _packageModelProvider.Package.Market = MarketGroupName;
        _packageModelProvider.Package.PackageIdentityName = PackageIdentityName;
        _packageModelProvider.Package.FolderSize = EstimatedFolderSize;
        _packageModelProvider.Package.UploadArguments = uploadArgs;
        _packageModelProvider.Package.MakePkg2Path = makePkg2Path;
        if (branchOrFlight != null)
        {
            _packageModelProvider.Package.BranchId = branchOrFlight.CurrentDraftInstanceId;
        }

        _windowService.NavigateTo(typeof(Msixvc2UploadingView));
    }

    internal string BuildUploadArguments()
    {
        var args = $"upload /d \"{ContentPath}\" /msixvc2";

        if (BranchOrFlightDisplayName.StartsWith("Branch: "))
        {
            string branchName = BranchOrFlightDisplayName[(BranchOrFlightDisplayName.IndexOf(':') + 2)..];
            args += $" /branch \"{branchName}\"";
        }
        else if (BranchOrFlightDisplayName.StartsWith("Flight: "))
        {
            string flightName = BranchOrFlightDisplayName[(BranchOrFlightDisplayName.IndexOf(':') + 2)..];
            args += $" /flight \"{flightName}\"";
        }

        if (!string.IsNullOrEmpty(MarketGroupName))
        {
            args += $" /market \"{MarketGroupName}\"";
        }

        if (!string.IsNullOrEmpty(BigId) && BigId != "None")
        {
            args += $" /storeid \"{BigId}\"";
        }

        args += " /auth CacheableBrowser";

        return args;
    }

    private void SetErrorAndGoToErrorPage(string title, string detail)
    {
        _errorModelProvider.Error.MainMessage = title;
        _errorModelProvider.Error.DetailMessage = detail;
        _errorModelProvider.Error.OriginPage = typeof(Msixvc2UploadView);
        _errorModelProvider.Error.LogsPath = string.Empty;
        _windowService.NavigateTo(typeof(ErrorPageView));
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
}
