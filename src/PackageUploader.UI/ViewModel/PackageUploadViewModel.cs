// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Windows.Input;
using System.Xml;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Exceptions;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using System.IO;
using PackageUploader.UI.Model;
using System.Windows.Media.Imaging;

namespace PackageUploader.UI.ViewModel;

public partial class PackageUploadViewModel : BaseViewModel
{
    private readonly PackageModelProvider _packageModelService;
    private readonly IPackageUploaderService _uploaderService;
    private readonly IWindowService _windowService;
    public readonly UploadingProgressPercentageProvider _uploadingProgressPercentageProvider;

    private GameProduct? _gameProduct = null;
    private IReadOnlyCollection<IGamePackageBranch>? _branchesAndFlights = null;
    private GamePackageConfiguration? _gamePackageConfiguration = null;

    private readonly OneTimeHolder<String>? _savedBranchOrFlightMemory = null;
    private readonly OneTimeHolder<String>? _savedMarketGroupMemory = null;

    private bool _isUploadInProgress = false;
    public bool IsUploadInProgress
    {
        get => _isUploadInProgress;
        set => SetProperty(ref _isUploadInProgress, value);
    }

    private string _branchOrFlightDisplayName = string.Empty;
    public string BranchOrFlightDisplayName
    {
        get => _branchOrFlightDisplayName;
        set
        {
            if (SetProperty(ref _branchOrFlightDisplayName, value))
            {
                _hasMarketGroups = false;
                UpdateMarketGroups();
            }
        }
    }

    private bool _hasMarketGroups = false;
    public bool HasMarketGroups
    {
        get => _hasMarketGroups;
        set => SetProperty(ref _hasMarketGroups, value);
    }

    private string _marketGroupName = string.Empty;
    public string MarketGroupName
    {
        get => _marketGroupName;
        set => SetProperty(ref _marketGroupName, value);
    }

    private string[] _branchAndFlightNames = [];
    public string[] BranchAndFlightNames
    {
        get => _branchAndFlightNames;
        set
        {
            if (SetProperty(ref _branchAndFlightNames, value))
            {
                string? branchOrFlightName = _savedBranchOrFlightMemory?.Value;
                if (!String.IsNullOrEmpty(branchOrFlightName) && String.IsNullOrEmpty(BranchOrFlightDisplayName))
                {
                    BranchOrFlightDisplayName = branchOrFlightName;
                }
                OnPropertyChanged(nameof(BranchOrFlightDisplayName));
            }
        }
    }

    private string[] _marketGroupNames = [];
    public string[] MarketGroupNames
    {
        get => _marketGroupNames;
        set
        {
            if (SetProperty(ref _marketGroupNames, value))
            {
                string? marketGroupName = _savedMarketGroupMemory?.Value;
                if (!String.IsNullOrEmpty(marketGroupName) && String.IsNullOrEmpty(MarketGroupName))
                {
                    MarketGroupName = marketGroupName;
                }
                OnPropertyChanged(nameof(MarketGroupName));
            }
        }
    }

    private async void UpdateMarketGroups()
    {
        if (_branchesAndFlights == null)
        {
            return;
        }

        IsLoadingMarkets = true;

        try
        {
            MarketGroupErrorMessage = string.Empty;

            IGamePackageBranch? branchOrFlight = GetBranchOrFlightFromUISelection();

            if (branchOrFlight != null)
            {
                _gamePackageConfiguration = await _uploaderService.GetPackageConfigurationAsync(_gameProduct, branchOrFlight, CancellationToken.None);

                List<string> marketNames = [];
                foreach (var market in _gamePackageConfiguration.MarketGroupPackages)
                {
                    marketNames.Add(market.Name);
                }
                MarketGroupNames = [.. marketNames];

                if (!string.IsNullOrEmpty(MarketGroupName) || !marketNames.Contains(MarketGroupName))
                {
                    MarketGroupName = MarketGroupNames.FirstOrDefault(string.Empty);
                }

                HasMarketGroups = true;

                CheckCanExecuteUploadCommand();
            }
            else
            {
                MarketGroupNames = [];
                MarketGroupName = string.Empty;

                MarketGroupErrorMessage = "No Branches found for this product";
            }
        }
        catch (Exception ex)
        {
            MarketGroupErrorMessage = $"Error getting market groups: {ex.Message}";
        }
        finally
        {
            IsLoadingMarkets = false;
        }

        CheckCanExecuteUploadCommand();
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

    //private double _progressValue = 0;
    public PackageUploadingProgressStages ProgressValue
    {
        get => _uploadingProgressPercentageProvider.UploadStage;
        set
        {
            if (_uploadingProgressPercentageProvider.UploadStage != value)
            {
                _uploadingProgressPercentageProvider.UploadStage = value;
                OnPropertyChanged();
            }
        }
    }

    public Model.PackageModel Package => _packageModelService.Package;
    
    // Package properties accessed from the shared service
    public string BigId 
    { 
        get => Package.BigId;
        set
        {
            if (Package.BigId != value)
            {
                Package.BigId = value;
                OnPropertyChanged();
                UpdatePackageState();
                SetPropertyInApplicationPreferences(nameof(BigId), value);

                GetProductInfoAsync();
            }
        }
    }

    public string PackageName
    {
        get
        {
            return string.IsNullOrEmpty(PackageFilePath) ? string.Empty : Path.GetFileName(PackageFilePath);
        }
    }

    public string ProductName
    {
        get
        {
            return _gameProduct == null ? string.Empty : _gameProduct.ProductName;
        }
    }

    public string PackageFilePath 
    { 
        get => Package.PackageFilePath;
        set
        {
            if (Package.PackageFilePath != value)
            {
                Package.PackageFilePath = value;
                ProcessSelectedPackage();
                SetPropertyInApplicationPreferences(nameof(PackageFilePath), value);
                OnPropertyChanged(nameof(PackageFilePath));
                OnPropertyChanged(nameof(PackageName));
            }
        }
    }
    
    public string EkbFilePath
    {
        get => Package.EkbFilePath;
        set
        {
            if (Package.EkbFilePath != value)
            {
                Package.EkbFilePath = value;
                SetPropertyInApplicationPreferences(nameof(EkbFilePath), value);
                OnPropertyChanged();
            }
        }
    }
    
    public string SubValFilePath
    {
        get => Package.SubValFilePath;
        set
        {
            if (Package.SubValFilePath != value)
            {
                Package.SubValFilePath = value;
                SetPropertyInApplicationPreferences(nameof(SubValFilePath), value);
                OnPropertyChanged();
            }
        }
    }

    public string SymbolBundleFilePath
    {
        get => Package.SymbolBundleFilePath;
        set
        {
            if (Package.SymbolBundleFilePath != value)
            {
                Package.SymbolBundleFilePath = value;
                SetPropertyInApplicationPreferences(nameof(SymbolBundleFilePath), value);
                OnPropertyChanged();
            }
        }
    }

    private string _packageId = string.Empty;
    public string PackageId
    {
        get => _packageId;
        set => SetProperty(ref _packageId, value);
    }

    //private string _packageSize = "Unknown";
    public string PackageSize
    {
        get => Package.PackageSize;//_packageSize;
        set
        { //=> SetProperty(ref _packageSize, value);
            if (Package.PackageSize != value)
            {
                Package.PackageSize = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isPackageUploadEnabled = true;
    public bool IsPackageUploadEnabled
    {
        get => _isPackageUploadEnabled;
        set 
        { 
            if (SetProperty(ref _isPackageUploadEnabled, value))
            {
                CheckCanExecuteUploadCommand();
            }
        }
    }

    private string _packageUploadTooltip = string.Empty;
    public string PackageUploadTooltip
    {
        get => _packageUploadTooltip;
        set => SetProperty(ref _packageUploadTooltip, value);
    }

    private bool _isLoadingBranchesAndFlights = false;
    public bool IsLoadingBranchesAndFlights
    {
        get => _isLoadingBranchesAndFlights;
        set => SetProperty(ref _isLoadingBranchesAndFlights, value);
    }

    private bool _isLoadingMarkets = false;
    public bool IsLoadingMarkets
    {
        get => _isLoadingMarkets;
        set => SetProperty(ref _isLoadingMarkets, value);
    }

    private string _packageErrorMessage = string.Empty;
    public string PackageErrorMessage
    {
        get => _packageErrorMessage;
        set => SetProperty(ref _packageErrorMessage, value);
    }

    private string _branchOrFlightErrorMessage = string.Empty;
    public string BranchOrFlightErrorMessage
    {
        get => _branchOrFlightErrorMessage;
        set => SetProperty(ref _branchOrFlightErrorMessage, value);
    }

    private string _marketGroupErrorMessage = string.Empty;
    public string MarketGroupErrorMessage
    {
        get => _marketGroupErrorMessage;
        set => SetProperty(ref _marketGroupErrorMessage, value);
    }

    //private BitmapImage? _packagePreviewImage = null;
    public BitmapImage? PackagePreviewImage
    {
        get => Package.PackagePreviewImage;//_packagePreviewImage;
        set
        { //=> SetProperty(ref _packagePreviewImage, value);
            if (Package.PackagePreviewImage != value)
            {
                Package.PackagePreviewImage = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand UploadPackageCommand { get; }
    public ICommand BrowseForPackageCommand { get; }
    public ICommand FileDroppedCommand { get; }
    public ICommand CancelUploadCommand { get; }
    public ICommand CancelButtonCommand { get; }

    private readonly string ConfileFilePath = Path.Combine(Path.GetTempPath(), $"PackageUploader_UI_GeneratedConfig_{DateTime.Now:yyyyMMddHHmmss}.log");

    private bool _hasValidPackage = false;
    public bool HasValidPackage
    {
        get => _hasValidPackage;
        private set 
        { 
            if (SetProperty(ref _hasValidPackage, value))
            {
                CheckCanExecuteUploadCommand();
            }
        }
    }

    private bool _isDragDropVisible = true;
    public bool IsDragDropVisible
    {
        get => _isDragDropVisible && !HasValidPackage;
        set => SetProperty(ref _isDragDropVisible, value);
    }

    private string _dragDropMessage = "Drag and drop a package file here or click to select";
    public string DragDropMessage
    {
        get => _dragDropMessage;
        set => SetProperty(ref _dragDropMessage, value);
    }
    private static readonly string[] packageExtensions = [".xvc", ".msixvc"];

    private CancellationTokenSource? _uploadCancellationTokenSource;

    public PackageUploadViewModel(PackageModelProvider packageModelService, 
                                  IPackageUploaderService uploaderService,
                                  IWindowService windowService,
                                  UploadingProgressPercentageProvider uploadingProgressPercentageProvider)
    {
        _packageModelService = packageModelService;
        _uploaderService = uploaderService;
        _windowService = windowService;
        _uploadingProgressPercentageProvider = uploadingProgressPercentageProvider;

        // Initialize commands with RelayCommand
        UploadPackageCommand = new RelayCommand(UploadPackageProcessAsync, () => IsUploadReady());
        BrowseForPackageCommand = new RelayCommand(BrowseForPackage);
        FileDroppedCommand = new RelayCommand<string>(ProcessDroppedFile);
        CancelUploadCommand = new RelayCommand(CancelUpload);
        CancelButtonCommand = new RelayCommand(OnCancelButton);

        string priorMarketGroup = GetPropertyFromApplicationPreferences(nameof(MarketGroupName));
        string priorBranchOrFlight = GetPropertyFromApplicationPreferences(nameof(BranchOrFlightDisplayName));
        _savedMarketGroupMemory = new OneTimeHolder<string>(priorMarketGroup); 
        _savedBranchOrFlightMemory = new OneTimeHolder<String>(priorBranchOrFlight);
        PackageSize = "Unknown"; // default

        if (string.IsNullOrEmpty(PackageFilePath))
        {
            PackageFilePath = GetPropertyFromApplicationPreferences(nameof(PackageFilePath));
        }
        else
        {
            ProcessSelectedPackage();
        }
    }

    private bool IsUploadReady()
    {
        return HasValidPackage &&
            _gameProduct != null &&
            !string.IsNullOrEmpty(MarketGroupName) &&
            !IsLoadingBranchesAndFlights &&
            !IsLoadingMarkets;
    }

    private void CheckCanExecuteUploadCommand()
    {
        // Force re-evaluation of the command's CanExecute status
        if (UploadPackageCommand is RelayCommand command)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    private void UpdatePackageState()
    {
        // Notify of changes to HasValidPackage and IsDragDropVisible when BigId changes
        OnPropertyChanged(nameof(HasValidPackage));
        OnPropertyChanged(nameof(IsDragDropVisible));
        CheckCanExecuteUploadCommand();
    }

    private void BrowseForPackage()
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Package Files (*.xvc;*.msixvc)|*.xvc;*.msixvc|All Files (*.*)|*.*",
                Title = "Select a Package File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Set the package file path
                PackageFilePath = openFileDialog.FileName;
            }
        }
        catch (Exception ex)
        {
            PackageErrorMessage = $"Error selecting file: {ex.Message}";
        }
    }

    private void ProcessDroppedFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            PackageErrorMessage = "Invalid file path.";
            return;
        }

        // Set the package file path
        PackageFilePath = filePath;
    }

    private void ProcessSelectedPackage()
    {
        // Reset package state
        ResetPackage();

        if (!File.Exists(PackageFilePath))
        {
            return;
        }
        
        // Check file extension
        string extension = Path.GetExtension(PackageFilePath).ToLowerInvariant();
        if (!(extension == ".xvc" || extension == ".msixvc"))
        {
            return;
        }
        
        try
        {            
            // Extract package information
            ExtractPackageInformation(PackageFilePath);

            // Update UI state
            if (!string.IsNullOrEmpty(BigId))
            {
                HasValidPackage = true;
            }

            OnPropertyChanged(nameof(IsDragDropVisible));
        }
        catch (Exception ex)
        {
            PackageErrorMessage = $"Error processing package: {ex.Message}";
        }
    }

    private void ResetPackage()
    {
        IsUploadInProgress = false;
        BigId = string.Empty;
        EkbFilePath = string.Empty;
        SubValFilePath = string.Empty;
        SymbolBundleFilePath = string.Empty;

        HasValidPackage = false;

        BranchAndFlightNames = [];
        MarketGroupNames = [];

        PackageErrorMessage = string.Empty;
        BranchOrFlightErrorMessage = string.Empty;
        MarketGroupErrorMessage = string.Empty;

        IsLoadingBranchesAndFlights = false;
        IsLoadingMarkets = false;
        OnPropertyChanged(nameof(IsDragDropVisible));
    }

    public void OnAppearing()
    {
        ProcessSelectedPackage();
    }

    private void ExtractPackageInformation(string packagePath)
    {
        try
        {
            XvcFile.GetBuildAndKeyId(packagePath, out Guid buildId, out Guid keyId);

            // Get just the filename without extension to build up other file paths
            string? baseFolder = Path.GetDirectoryName(packagePath);

            if (!Directory.Exists(baseFolder))
            {
                throw new DirectoryNotFoundException($"Directory not found: {baseFolder}");
            }
            string fileName = Path.GetFileNameWithoutExtension(packagePath);

            EkbFilePath = Path.Combine(baseFolder, $"{fileName}_Full_{keyId}.ekb");
            SubValFilePath = Path.Combine(baseFolder, $"Validator_{fileName}.xml");

            // Parse the SubVal file for any remaining needed fields. Use the buildId to verify it's the right Validator log.
            ExtractIdInformationFromValidatorLog(buildId, out string? titleId, out string storeId, out string logoFilename);

            var symbolBundleFilePath = string.Empty;
            if (!string.IsNullOrEmpty(titleId))
            {
                symbolBundleFilePath = Path.Combine(baseFolder, $"{fileName}_{titleId}.zip");
            }
            else
            {
                symbolBundleFilePath = Path.Combine(baseFolder, $"{fileName}.zip");
            }

            if (File.Exists(symbolBundleFilePath))
            {
                SymbolBundleFilePath = symbolBundleFilePath;
            }
            else
            {
                SymbolBundleFilePath = string.Empty;
            }

            if (!string.IsNullOrEmpty(storeId))
            {
                BigId = storeId;
            }
            else
            {
                PackageErrorMessage = $"Package has no StoreId/BigId. Configure your StoreId in the MicrosoftGame.config file before building your package.";
            }

            // Update package preview information
            PackageId = fileName;
            FileInfo fileInfo = new(packagePath);

            double bytesInMB = 1024.0 * 1024.0;
            double bytesInGB = bytesInMB * 1024.0;
            if (fileInfo.Length > bytesInGB)
            {
                PackageSize = string.Format("{0:0.##} GB", fileInfo.Length / bytesInGB);
            }
            else
            {
                PackageSize = string.Format("{0:0.##} MB", fileInfo.Length / bytesInMB);
            }

            if (!string.IsNullOrEmpty(logoFilename))
            {
                XvcFile.ExtractFile(packagePath, logoFilename, out byte[]? fileContents);
                PackagePreviewImage = fileContents != null ? LoadBitmapImage(fileContents) : null;
            }
        }
        catch (Exception ex)
        {
            PackageErrorMessage = $"Error extracting package information: {ex.Message}";
        }
    }

    private static BitmapImage? LoadBitmapImage(byte[] fileContents)
    {
        BitmapImage bitmapImage = new();
        using (MemoryStream stream = new(fileContents))
        {
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
        }
        return bitmapImage;
    }

    private void ExtractIdInformationFromValidatorLog(Guid expectedBuildId, out string? titleId, out string storeId, out string logoFilename)
    {
        // Read the XML file and extract the TitleId and StoreId
        if (!File.Exists(SubValFilePath))
        {
            throw new FileNotFoundException($"Submission Validator log not found: {SubValFilePath}");
        }

        using FileStream stream = File.OpenRead(SubValFilePath);
        using XmlReader reader = XmlReader.Create(stream);

        XmlDocument? xmlDoc = new();
        xmlDoc.Load(reader);

        XmlNode? node = xmlDoc.SelectSingleNode("//project/BuildId");
        if (node != null)
        {
            Guid buildId = new(node.InnerText);

            if (buildId != expectedBuildId)
            {
                throw new Exception($"BuildId mismatch in the Submission Validator log file. Expected: {expectedBuildId}, Found: {buildId}");
            }
        }

        node = xmlDoc.SelectSingleNode("//GameConfig/Game/StoreId");
        storeId = node?.InnerText ?? string.Empty;

        node = xmlDoc.SelectSingleNode("//GameConfig/Game/TitleId");
        titleId = node?.InnerText ?? string.Empty;

        node = xmlDoc.SelectSingleNode("//GameConfig/Game/ShellVisuals");
        logoFilename = node?.Attributes?.GetNamedItem("Square150x150Logo")?.InnerText ?? string.Empty;
    }

    private async void GetProductInfoAsync()
    {
        if (string.IsNullOrEmpty(BigId))
        {
            return;
        }

        if (!IsPackageUploadEnabled)
        {
            return;
        }

        IsLoadingBranchesAndFlights = true;
        try
        {
            _gameProduct = await _uploaderService.GetProductByBigIdAsync(BigId, CancellationToken.None);

            OnPropertyChanged(nameof(ProductName));

            _branchesAndFlights = await _uploaderService.GetPackageBranchesAsync(_gameProduct, CancellationToken.None);

            List<string> displayNames = [];
            foreach (var branch in _branchesAndFlights)
            {
                if (branch.BranchType == GamePackageBranchType.Branch)
                {
                    displayNames.Add("Branch: " + branch.Name);
                }
                else if (branch.BranchType == GamePackageBranchType.Flight)
                {
                    displayNames.Add("Flight: " + branch.Name);
                }
            }

            BranchAndFlightNames = [.. displayNames];

            if (string.IsNullOrEmpty(BranchOrFlightDisplayName) || !displayNames.Contains(BranchOrFlightDisplayName))
            {
                BranchOrFlightDisplayName = BranchAndFlightNames.FirstOrDefault(string.Empty);
            }
            OnPropertyChanged(nameof(BranchOrFlightDisplayName));

            CheckCanExecuteUploadCommand();
        }
        catch (ProductNotFoundException)
        {
            BranchOrFlightErrorMessage = $"Product with BigId '{BigId}' not found. Configure your product at https://partner.microsoft.com/";
        }
        catch (Exception ex)
        {
            BranchOrFlightErrorMessage = $"Error getting product information: {ex.Message}";
        }
        finally
        {
            IsLoadingBranchesAndFlights = false;
        }
    }

    private async void UploadPackageProcessAsync()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _windowService.NavigateTo(typeof(PackageUploadingView));
        });

        if (_gameProduct == null || _branchesAndFlights == null || _gamePackageConfiguration == null)
        {
            PackageErrorMessage = "Product information not available. Please enter a valid BigId and try again.";
            return;
        }

        ProgressValue = 0;
        IsUploadInProgress = true;

        // We generate an uploader config for debugging or CLI use
        GenerateUploaderConfig();

        // Find the branch with BranchFriendlyName or FlightName
        IGamePackageBranch? branchOrFlight = GetBranchOrFlightFromUISelection();

        if (branchOrFlight == null)
        {
            PackageErrorMessage = $"Branch '{BranchOrFlightDisplayName}' not found.";
            IsUploadInProgress = false;
            return;
        }

        var timer = new Stopwatch();
        timer.Start();

        _uploadCancellationTokenSource = new CancellationTokenSource();
        var ct = _uploadCancellationTokenSource.Token;

        try
        {
            var marketGroupPackage = _gamePackageConfiguration.MarketGroupPackages.SingleOrDefault(x => x.Name.Equals(MarketGroupName));

            IProgress<PackageUploadingProgressStages> progress = new Progress<PackageUploadingProgressStages>(value => { 
                ProgressValue = value; 
            });

            GamePackage gamePackage = await _uploaderService.UploadGamePackageAsync(
                _gameProduct,
                branchOrFlight,
                marketGroupPackage,
                PackageFilePath,
                new ClientApi.Models.GameAssets { EkbFilePath = EkbFilePath, SubValFilePath = SubValFilePath, SymbolsFilePath = SymbolBundleFilePath },
                minutesToWaitForProcessing: 60,
                deltaUpload: true,
                isXvc: true,
                progress,
                ct);

            ProgressValue = PackageUploadingProgressStages.Done;
            timer.Stop();
            // SuccessMessage = $"Package uploaded successfully in {timer.Elapsed:hh\\:mm\\:ss}.";
            
            // After successful upload, navigate back to main page
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.NavigateTo(typeof(UploadingFinishedView));
            });
        }
        catch (OperationCanceledException)
        {
            PackageErrorMessage = "Upload cancelled.";
        }
        catch (Exception ex)
        {
            PackageErrorMessage = $"Error uploading package: {ex.Message}";
        }
        finally
        {
            IsUploadInProgress = false;
            _uploadCancellationTokenSource = null;
        }
    }

    private void CancelUpload()
    {
        _uploadCancellationTokenSource?.Cancel();
    }

    private void GenerateUploaderConfig()
    {
        if (_branchesAndFlights == null)
        {
            return;
        }
        var branchOrFlight = _branchesAndFlights.FirstOrDefault(x => x.Name == BranchOrFlightDisplayName);

        if (branchOrFlight == null)
        {
            return;
        }

        var config = new Model.UploadConfig
        {
            bigId = BigId,
            branchFriendlyName = branchOrFlight.BranchType == GamePackageBranchType.Branch ? branchOrFlight.Name : string.Empty,
            flightName = branchOrFlight.BranchType == GamePackageBranchType.Flight ? branchOrFlight.Name : string.Empty,
            packageFilePath = PackageFilePath,
            marketGroupName = MarketGroupName,
            gameAssets = new Model.GameAssets
            {
                ekbFilePath = EkbFilePath,
                subValFilePath = SubValFilePath
            }
        };

        string configFileText = System.Text.Json.JsonSerializer.Serialize(config);
        File.WriteAllText(ConfileFilePath, configFileText);
    }

    private void OnCancelButton()
    {
        // Navigate to the main page view
        _windowService.NavigateTo(typeof(MainPageView));
    }
}
