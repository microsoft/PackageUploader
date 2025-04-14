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

namespace PackageUploader.UI.ViewModel;

public partial class PackageUploadViewModel : BaseViewModel
{
    private readonly PackageModelProvider _packageModelService;
    private readonly IPackageUploaderService _uploaderService;
    private readonly PackageUploader.UI.Utility.IWindowService _windowService;

    private GameProduct? _gameProduct = null;
    private IReadOnlyCollection<IGamePackageBranch>? _branchesAndFlights = null;
    private GamePackageConfiguration? _gamePackageConfiguration = null;

    private OneTimeHolder<String>? _savedBranchMemory = null;
    private OneTimeHolder<String>? _savedFlightMemory = null;

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

    private string _branchFriendlyName = string.Empty;
    public string BranchFriendlyName
    {
        get => _branchFriendlyName;
        set
        {
            if (SetProperty(ref _branchFriendlyName, value))
            {
                UpdateMarketGroups();
            }
        }
    }

    private string _marketGroupName = string.Empty;
    public string MarketGroupName
    {
        get => _marketGroupName;
        set => SetProperty(ref _marketGroupName, value);
    }

    private string[] _branchFriendlyNames = [];
    public string[] BranchFriendlyNames
    {
        get => _branchFriendlyNames;
        set
        {
            if (SetProperty(ref _branchFriendlyNames, value))
            {
                string? branchName = _savedBranchMemory?.Value;
                if (!String.IsNullOrEmpty(branchName) && String.IsNullOrEmpty(BranchFriendlyName))
                {
                    BranchFriendlyName = branchName;
                }
                OnPropertyChanged(nameof(BranchFriendlyName));
            }
        }
    }

    private string _flightName = string.Empty;
    public string FlightName
    {
        get => _flightName;
        set
        {
            if (SetProperty(ref _flightName, value))
            {
                UpdateMarketGroups();
            }
        }
    }

    private string[] _flightNames = [];
    public string[] FlightNames
    {
        get => _flightNames;
        set
        {
            if (SetProperty(ref _flightNames, value))
            {
                OnPropertyChanged(nameof(FlightName));
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

            IGamePackageBranch? branchOrFlight = null;

            if (IsBranchSelected)
            {
                branchOrFlight = _branchesAndFlights.FirstOrDefault(x => x.Name == BranchFriendlyName);
            }
            else
            {
                branchOrFlight = _branchesAndFlights.FirstOrDefault(x => x.Name == FlightName);
            }

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

                CheckCanExecuteUploadCommand();
            }
            else
            {
                MarketGroupNames = [];
                MarketGroupName = string.Empty;

                if (IsBranchSelected)
                {
                    ErrorMessage = "No Branches found for this product";
                }
                else
                {
                    ErrorMessage = "No Flight Groups found for this product";
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error getting market groups: {ex.Message}";
        }

        CheckCanExecuteUploadCommand();
        IsSpinnerRunning = false;
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

    public string PackageFilePath 
    { 
        get => Package.PackageFilePath;
        set
        {
            if (Package.PackageFilePath != value)
            {
                Package.PackageFilePath = value;
                SetPropertyInApplicationPreferences(nameof(PackageFilePath), value);
                OnPropertyChanged();
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

    private readonly string ConfileFilePath = Path.Combine(Path.GetTempPath(), $"PackageUploader_UI_GeneratedConfig_{DateTime.Now:yyyyMMddHHmmss}.log");

    private bool _hasPackage = false;
    public bool HasPackage
    {
        get => _hasPackage || !string.IsNullOrEmpty(BigId);
        private set 
        { 
            if (SetProperty(ref _hasPackage, value))
            {
                CheckCanExecuteUploadCommand();
            }
        }
    }

    private bool _isDragDropVisible = true;
    public bool IsDragDropVisible
    {
        get => _isDragDropVisible && !HasPackage;
        set => SetProperty(ref _isDragDropVisible, value);
    }

    private string _dragDropMessage = "Drag and drop a package file here or click to select";
    public string DragDropMessage
    {
        get => _dragDropMessage;
        set => SetProperty(ref _dragDropMessage, value);
    }
    private static readonly string[] packageExtensions = [".xvc", ".msixvc"];

    private string _selectedMode = "Branch";
    public string SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetProperty(ref _selectedMode, value))
            {
                UpdateMarketGroups();
                OnPropertyChanged(nameof(IsBranchSelected));
                OnPropertyChanged(nameof(IsFlightSelected));
            }
        }
    }

    public bool IsBranchSelected => SelectedMode == "Branch";
    public bool IsFlightSelected => SelectedMode == "Flight";

    public List<string> Modes { get; } = ["Branch", "Flight"];

    private CancellationTokenSource? _uploadCancellationTokenSource;

    public PackageUploadViewModel(
        PackageModelProvider packageModelService, 
        IPackageUploaderService uploaderService,
        PackageUploader.UI.Utility.IWindowService windowService)
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

        SelectedMode = GetPropertyFromApplicationPreferences(nameof(SelectedMode));
        MarketGroupName = GetPropertyFromApplicationPreferences(nameof(MarketGroupName));
        FlightName = GetPropertyFromApplicationPreferences(nameof(FlightName));
        _savedFlightMemory = new OneTimeHolder<String>(FlightName);
        BranchFriendlyName = GetPropertyFromApplicationPreferences(nameof(BranchFriendlyName));
        _savedBranchMemory = new OneTimeHolder<String>(BranchFriendlyName);
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
        return HasPackage && _gameProduct != null && !string.IsNullOrEmpty(MarketGroupName);
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
        // Notify of changes to HasPackage and IsDragDropVisible when BigId changes
        OnPropertyChanged(nameof(HasPackage));
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
                HasPackage = true;
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
                ProcessSelectedPackage(openFileDialog.FileName);
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
        
        ProcessSelectedPackage(filePath);
    }

    private void ProcessSelectedPackage(string filePath)
    {
        if (!File.Exists(filePath))
        {
            ErrorMessage = "File does not exist.";
            return;
        }
        
        // Check file extension
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!(extension == ".xvc" || extension == ".msixvc"))
        {
            ErrorMessage = "Invalid package format. Please select an .xvc or .msixvc file.";
            return;
        }
        
        // Clear any previous errors or success messages
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        
        try
        {
            // Set the package file path
            PackageFilePath = filePath;
            
            // Extract package information
            ExtractPackageInformation(filePath);

            // Update UI state
            if (!string.IsNullOrEmpty(BigId))
            {
                HasPackage = true;
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

        HasPackage = false;
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

            List<string> branchNames = [];
            List<string> flightNames = [];
            foreach (var branch in _branchesAndFlights)
            {
                if (branch.BranchType == GamePackageBranchType.Branch)
                {
                    branchNames.Add(branch.Name);
                }
                else if (branch.BranchType == GamePackageBranchType.Flight)
                {
                    flightNames.Add(branch.Name);
                }
            }
            BranchFriendlyNames = [.. branchNames];
            FlightNames = [.. flightNames];

            if(string.IsNullOrEmpty(BranchFriendlyName) || !branchNames.Contains(BranchFriendlyName))
            {
                BranchFriendlyName = BranchFriendlyNames.FirstOrDefault(string.Empty);
            }
            if(string.IsNullOrEmpty(FlightName) || !flightNames.Contains(FlightName))
            {
                FlightName = FlightNames.FirstOrDefault(string.Empty);
            }
            OnPropertyChanged(nameof(BranchFriendlyName));
            OnPropertyChanged(nameof(FlightName));

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
        IGamePackageBranch? branchOrFlight = null;

        if (IsBranchSelected)
        {
            branchOrFlight = _branchesAndFlights.FirstOrDefault(x => x.Name == BranchFriendlyName);
        }
        else
        {
            branchOrFlight = _branchesAndFlights.FirstOrDefault(x => x.Name == FlightName);
        }

        if (branchOrFlight == null)
        {
            if (IsFlightSelected)
            {
                if (string.IsNullOrEmpty(FlightName))
                {
                    ErrorMessage = "Selected product has no Flights";
                }
                else
                {
                    ErrorMessage = $"Flight '{FlightName}' not found.";
                }
            }
            else
            {
                ErrorMessage = $"Branch '{BranchFriendlyName}' not found.";
            }
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
        var config = new Model.UploadConfig
        {
            bigId = BigId,
            branchFriendlyName = BranchFriendlyName,
            flightName = FlightName,
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
}
