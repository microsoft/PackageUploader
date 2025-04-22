// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Windows.Input;
using System.Xml;
using Microsoft.Win32;
using PackageUploader.ClientApi;
using PackageUploader.ClientApi.Client.Ingestion.Exceptions;
using PackageUploader.ClientApi.Client.Ingestion.Models;
using PackageUploader.ClientApi.Models;
using PackageUploader.UI.Providers;
using PackageUploader.UI.Utility;
using PackageUploader.UI.View;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;

namespace PackageUploader.UI.ViewModel;

public partial class PackageUploadViewModel : BaseViewModel
{
    private readonly PackageModelProvider _packageModelService;
    private readonly IPackageUploaderService _uploaderService;
    private readonly IWindowService _windowService;

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

    private bool _isSpinnerRunning = false;
    public bool IsSpinnerRunning
    {
        get => _isSpinnerRunning;
        set => SetProperty(ref _isSpinnerRunning, value);
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

        IsSpinnerRunning = true;
        try
        {
            ErrorMessage = string.Empty;

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

                ErrorMessage = "No Branches found for this product";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error getting market groups: {ex.Message}";
        }

        CheckCanExecuteUploadCommand();
        IsSpinnerRunning = false;
    }

    private IGamePackageBranch? GetBranchOrFlightFromUISelection()
    {
        if (_branchesAndFlights == null)
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

    private double _progressValue = 0;
    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    private bool _isProgressVisible = false;
    public bool IsProgressVisible
    {
        get => _isProgressVisible;
        set => SetProperty(ref _isProgressVisible, value);
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

    public string PackageFilePath 
    { 
        get => Package.PackageFilePath;
        set
        {
            if (Package.PackageFilePath != value)
            {
                Package.PackageFilePath = value;
                ProcessSelectedPackage(Package.PackageFilePath);
                SetPropertyInApplicationPreferences(nameof(PackageFilePath), value);
                OnPropertyChanged(nameof(PackageFilePath));
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

    private BitmapImage? _packagePreviewImage = null;
    public BitmapImage? PackagePreviewImage
    {
        get => _packagePreviewImage;
        set => SetProperty(ref _packagePreviewImage, value);
    }

    private string _packageId = string.Empty;
    public string PackageId
    {
        get => _packageId;
        set => SetProperty(ref _packageId, value);
    }

    private string _packageSize = "Unknown";
    public string PackageSize
    {
        get => _packageSize;
        set => SetProperty(ref _packageSize, value);
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

    private string _successMessage = string.Empty;
    public string SuccessMessage
    {
        get => _successMessage;
        set => SetProperty(ref _successMessage, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand UploadPackageCommand { get; }
    public ICommand BrowseForPackageCommand { get; }
    public ICommand FileDroppedCommand { get; }
    public ICommand ResetPackageCommand { get; }
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
                                  IWindowService windowService)
    {
        _packageModelService = packageModelService;
        _uploaderService = uploaderService;
        _windowService = windowService;

        // Initialize commands with RelayCommand
        UploadPackageCommand = new RelayCommand(UploadPackageProcessAsync, () => IsUploadReady());
        BrowseForPackageCommand = new RelayCommand(BrowseForPackage);
        FileDroppedCommand = new RelayCommand<string>(ProcessDroppedFile);
        ResetPackageCommand = new RelayCommand(ResetPackage);
        CancelUploadCommand = new RelayCommand(CancelUpload);
        CancelButtonCommand = new RelayCommand(OnCancelButton);

        string priorMarketGroup = GetPropertyFromApplicationPreferences(nameof(MarketGroupName));
        string priorBranchOrFlight = GetPropertyFromApplicationPreferences(nameof(BranchOrFlightDisplayName));
        _savedMarketGroupMemory = new OneTimeHolder<string>(priorMarketGroup); 
        _savedBranchOrFlightMemory = new OneTimeHolder<String>(priorBranchOrFlight);

        if (string.IsNullOrEmpty(PackageFilePath))
        {
            PackageFilePath = GetPropertyFromApplicationPreferences(nameof(PackageFilePath));
            if (!string.IsNullOrEmpty(PackageFilePath))
            {
                IsDragDropVisible = false;
                ExtractPackageInformation(PackageFilePath);
                UpdateMarketGroups();
            }
        }
    }

    private bool IsUploadReady()
    {
        return HasValidPackage && _gameProduct != null && !string.IsNullOrEmpty(MarketGroupName);
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

    public void OnAppearing()
    {
        // Update UI based on any changed package data
        if (!string.IsNullOrEmpty(PackageFilePath) && File.Exists(PackageFilePath))
        {
            ExtractPackageInformation(PackageFilePath);

            if (!string.IsNullOrEmpty(BigId))
            {
                HasValidPackage = true;
            }

            // Refresh all bound properties from the shared model
            OnPropertyChanged(nameof(BigId));
            OnPropertyChanged(nameof(PackageFilePath));
            OnPropertyChanged(nameof(EkbFilePath));
            OnPropertyChanged(nameof(SubValFilePath));
            OnPropertyChanged(nameof(SymbolBundleFilePath));
            OnPropertyChanged(nameof(IsDragDropVisible));
        }

        // Clear any previous messages
        IsProgressVisible = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
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
            ErrorMessage = $"Error selecting file: {ex.Message}";
        }
    }

    private void ProcessDroppedFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            ErrorMessage = "Invalid file path.";
            return;
        }

        // Set the package file path
        PackageFilePath = filePath;
    }

    private void ProcessSelectedPackage(string filePath)
    {
        HasValidPackage = false;

        if (!File.Exists(filePath))
        {
            return;
        }
        
        // Check file extension
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!(extension == ".xvc" || extension == ".msixvc"))
        {
            return;
        }
        
        try
        {            
            // Extract package information
            ExtractPackageInformation(filePath);

            // Update UI state
            if (!string.IsNullOrEmpty(BigId))
            {
                HasValidPackage = true;
            }

            OnPropertyChanged(nameof(IsDragDropVisible));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error processing package: {ex.Message}";
        }
    }

    private void ResetPackage()
    {
        IsSpinnerRunning = false;
        IsUploadInProgress = false;
        IsProgressVisible = false;
        BigId = string.Empty;
        PackageFilePath = string.Empty;
        EkbFilePath = string.Empty;
        SubValFilePath = string.Empty;
        SymbolBundleFilePath = string.Empty;

        HasValidPackage = false;
        OnPropertyChanged(nameof(IsDragDropVisible));
        
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private void ExtractPackageInformation(string packagePath)
    {
        try
        {
            Model.XvcFile.GetBuildAndKeyId(packagePath, out Guid buildId, out Guid keyId);

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
            ExtractIdInformationFromValidatorLog(buildId, out string? titleId, out string storeId);

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
                ErrorMessage = $"Package has no StoreId/BigId. Configure your StoreId in the MicrosoftGame.config file before building your package.";
            }

            // Update package preview information
            PackageId = fileName;
            FileInfo fileInfo = new(packagePath);
            PackageSize = string.Format("{0:0.##} MB", fileInfo.Length / (1024.0 * 1024.0));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error extracting package information: {ex.Message}";
        }
    }

    private void ExtractIdInformationFromValidatorLog(Guid expectedBuildId, out string? titleId, out string storeId)
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

        IsSpinnerRunning = true;
        try
        {
            _gameProduct = await _uploaderService.GetProductByBigIdAsync(BigId, CancellationToken.None);
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
            ErrorMessage = string.Empty;
        }
        catch (ProductNotFoundException)
        {
            ErrorMessage = $"Product with BigId '{BigId}' not found. Configure your product at https://partner.microsoft.com/";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error getting product information: {ex.Message}";
        }

        IsSpinnerRunning = false;
    }

    private async void UploadPackageProcessAsync()
    {
        // Clear any previous status messages when starting a new upload
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;

        if (_gameProduct == null || _branchesAndFlights == null || _gamePackageConfiguration == null)
        {
            ErrorMessage = "Product information not available. Please enter a valid BigId and try again.";
            return;
        }

        ProgressValue = 0;
        IsSpinnerRunning = true;
        IsUploadInProgress = true;
        IsProgressVisible = true;

        // We generate an uploader config for debugging or CLI use
        GenerateUploaderConfig();

        // Find the branch with BranchFriendlyName or FlightName
        IGamePackageBranch? branchOrFlight = GetBranchOrFlightFromUISelection();

        if (branchOrFlight == null)
        {
            ErrorMessage = $"Branch '{BranchOrFlightDisplayName}' not found.";
            IsSpinnerRunning = false;
            IsProgressVisible = false;
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

            IProgress<double> progress = new Progress<double>(value => ProgressValue = value);

            GamePackage gamePackage = await _uploaderService.UploadGamePackageAsync(
                _gameProduct,
                branchOrFlight,
                marketGroupPackage,
                PackageFilePath,
                new GameAssets { EkbFilePath = EkbFilePath, SubValFilePath = SubValFilePath, SymbolsFilePath = SymbolBundleFilePath },
                minutesToWaitForProcessing: 60,
                deltaUpload: true,
                isXvc: true,
                progress,
                ct);

            ProgressValue = 1;
            timer.Stop();
            SuccessMessage = $"Package uploaded successfully in {timer.Elapsed:hh\\:mm\\:ss}.";
            
            // After successful upload, navigate back to main page
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.NavigateTo(typeof(MainPageView));
            });
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Upload cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error uploading package: {ex.Message}";
        }
        finally
        {
            IsSpinnerRunning = false;
            IsUploadInProgress = false;
            IsProgressVisible = false;
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
