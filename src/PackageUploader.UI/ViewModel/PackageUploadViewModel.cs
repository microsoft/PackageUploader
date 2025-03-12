// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Xml;
using PackageUploader.UI.Model;
using PackageUploader.UI.Services;

namespace PackageUploader.UI.ViewModel;

public partial class PackageUploadViewModel : BaseViewModel
{
    private readonly PackageModelService _packageModelService;
    private readonly PathConfigurationService _pathConfigurationService;

    private bool _isSpinnerRunning = false;
    public bool IsSpinnerRunning
    {
        get => _isSpinnerRunning;
        set => SetProperty(ref _isSpinnerRunning, value);
    }

    private string _branchFriendlyName = "Main";
    public string BranchFriendlyName
    {
        get => _branchFriendlyName;
        set => SetProperty(ref _branchFriendlyName, value);
    }

    private string _marketGroupName = "default";
    public string MarketGroupName
    {
        get => _marketGroupName;
        set => SetProperty(ref _marketGroupName, value);
    }

    private string[] _branchFriendlyNames = ["Main"];
    public string[] BranchFriendlyNames
    {
        get => _branchFriendlyNames;
        set
        {
            if (SetProperty(ref _branchFriendlyNames, value))
            {
                OnPropertyChanged(nameof(BranchFriendlyName));
            }
        }
    }

    private string _flightName = string.Empty;
    public string FlightName
    {
        get => _flightName;
        set => SetProperty(ref _flightName, value);
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
    
    public PackageModel Package => _packageModelService.Package;
    
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
                (UploadPackageCommand as Command)?.ChangeCanExecute();
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

    private readonly string ConfileFilePath = Path.Combine(Path.GetTempPath(), $"PackageUploader_UI_GeneratedConfig_{DateTime.Now:yyyyMMddHHmmss}.log");
    private readonly ArrayList processOutput = [];
    private Process? packageUploaderProcess;

    private bool _hasPackage = false;
    public bool HasPackage
    {
        get => _hasPackage || !string.IsNullOrEmpty(BigId);
        private set 
        { 
            if (SetProperty(ref _hasPackage, value))
            {
                (UploadPackageCommand as Command)?.ChangeCanExecute();
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

    public PackageUploadViewModel(PackageModelService packageModelService, PathConfigurationService pathConfigurationService)
    {
        _packageModelService = packageModelService;
        _pathConfigurationService = pathConfigurationService;

        if (File.Exists(_pathConfigurationService.PackageUploaderPath))
        {
            IsPackageUploadEnabled = true;
        }
        else
        {
            IsPackageUploadEnabled = false;
            ErrorMessage = "PackageUploader.exe was not found. Package upload is unavailable.";
        }

        // Initialize commands
        UploadPackageCommand = new Command(
            StartUploadPackageProcess, 
            () => IsPackageUploadEnabled && HasPackage);
        BrowseForPackageCommand = new Command(async () => await BrowseForPackage());
        FileDroppedCommand = new Command<string>(ProcessDroppedFile);
        ResetPackageCommand = new Command(ResetPackage);
    }

    private void UpdatePackageState()
    {
        // Notify of changes to HasPackage and IsDragDropVisible when BigId changes
        OnPropertyChanged(nameof(HasPackage));
        OnPropertyChanged(nameof(IsDragDropVisible));
        (UploadPackageCommand as Command)?.ChangeCanExecute();
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
                StartGetProductInfo();
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
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private async Task BrowseForPackage()
    {
        try
        {
            var fileTypes = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, packageExtensions },
                    { DevicePlatform.MacCatalyst, packageExtensions },
                });

            var options = new PickOptions
            {
                PickerTitle = "Select a Package File",
                FileTypes = fileTypes,
            };

            var result = await FilePicker.Default.PickAsync(options);
            
            if (result != null)
            {
                ProcessSelectedPackage(result.FullPath);
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
                StartGetProductInfo();
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

            BigId = storeId;
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

    private void StartGetProductInfo()
    {
        if (string.IsNullOrEmpty(BigId))
        {
            return;
        }

        if (!IsPackageUploadEnabled)
        {
            return;
        }
        
        var config = new GetProductConfig
        {
            bigId = BigId
        };
        string configFileText = System.Text.Json.JsonSerializer.Serialize(config);
        File.WriteAllText(ConfileFilePath, configFileText);

        // Provide a file path so we can distinguish between UI and CLI flows.
        string logFilePath = Path.Combine(Path.GetTempPath(), $"PackageUploader_UI_GetInfo_{DateTime.Now:yyyyMMddHHmmss}.log");
        string pkgBuildExePath = _pathConfigurationService.PackageUploaderPath;
        string cmdFormat = $"GetProduct -c {ConfileFilePath} -a default -l {logFilePath}";

        packageUploaderProcess = new Process();
        packageUploaderProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(pkgBuildExePath);
        packageUploaderProcess.StartInfo.FileName = pkgBuildExePath;
        packageUploaderProcess.StartInfo.Arguments = cmdFormat;
        packageUploaderProcess.StartInfo.RedirectStandardOutput = true;
        packageUploaderProcess.StartInfo.RedirectStandardError = true;
        packageUploaderProcess.EnableRaisingEvents = true;
        packageUploaderProcess.StartInfo.CreateNoWindow = true;

        packageUploaderProcess.OutputDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };

        packageUploaderProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };

        packageUploaderProcess.Exited += (sender, args) =>
        {
            IsSpinnerRunning = false;
            string outputString = string.Join("\n", processOutput.ToArray());

            // Parse Get Product Output
            ProcessGetProductOutput(outputString);
            packageUploaderProcess.WaitForExit();
        };

        packageUploaderProcess.Start();
        packageUploaderProcess.BeginOutputReadLine();
        packageUploaderProcess.BeginErrorReadLine();
        IsSpinnerRunning = true;
    }

    [GeneratedRegex(@"PackageUploader\.Application\.Operations\.GetProductOperation\[0\] Product: \{(?<ProductResponse>.*?)\}")]
    private static partial Regex ProductResponseRegex();

    private void ProcessGetProductOutput(string outputString)
    {
        if (outputString == null)
        {
            return;
        }

        string? productResponseString = null;
        MatchCollection productResponseCollection = ProductResponseRegex().Matches(outputString);

        for (int i = 0; i < productResponseCollection.Count; i++)
        {
            productResponseString = "{" + productResponseCollection[i].Groups["ProductResponse"].Value + "}";
            if (productResponseString != null)
            {
                break;
            }
        }

        if (productResponseString == null)
        {
            return;
        }

        GetProductResponse? response = System.Text.Json.JsonSerializer.Deserialize<GetProductResponse>(productResponseString);

        if (response != null)
        {
            BranchFriendlyNames = response.branchFriendlyNames;
            FlightNames = response.flightNames;
        }
    }

    private void StartUploadPackageProcess()
    {
        // Clear any previous status messages when starting a new upload
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;
        
        IsSpinnerRunning = true;

        var config = new UploadConfig
        {
            bigId = BigId,
            branchFriendlyName = BranchFriendlyName,
            flightName = FlightName,
            packageFilePath = PackageFilePath,
            marketGroupName = MarketGroupName,
            gameAssets = new GameAssets
            {
                ekbFilePath = EkbFilePath,
                subValFilePath = SubValFilePath
            }
        };

        string configFileText = System.Text.Json.JsonSerializer.Serialize(config);
        File.WriteAllText(ConfileFilePath, configFileText);

        // Provide a file path so we can distinguish between UI and CLI flows.
        string logFilePath = Path.Combine(Path.GetTempPath(), $"PackageUploader_UI_{DateTime.Now:yyyyMMddHHmmss}.log");
        string pkgBuildExePath = _pathConfigurationService.PackageUploaderPath;
        string cmdFormat = $"UploadXvcPackage -c {ConfileFilePath} -a default -l {logFilePath}";
        packageUploaderProcess = new Process();
        packageUploaderProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(pkgBuildExePath);
        packageUploaderProcess.StartInfo.FileName = pkgBuildExePath;
        packageUploaderProcess.StartInfo.Arguments = cmdFormat;
        packageUploaderProcess.StartInfo.RedirectStandardOutput = true;
        packageUploaderProcess.StartInfo.RedirectStandardError = true;
        packageUploaderProcess.EnableRaisingEvents = true;
        packageUploaderProcess.StartInfo.CreateNoWindow = true;
        packageUploaderProcess.OutputDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };
        packageUploaderProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };
        packageUploaderProcess.Exited += (sender, args) =>
        {
            string outputString = string.Join("\n", processOutput.ToArray());
            packageUploaderProcess.WaitForExit();

            if (packageUploaderProcess.ExitCode == 0)
            {
                SuccessMessage = "Package uploaded successfully!";
            }
            else
            {
                ErrorMessage = $"Package upload failed with exit code: {packageUploaderProcess.ExitCode}";
            }

            IsSpinnerRunning = false;
        };
        packageUploaderProcess.Start();
        packageUploaderProcess.BeginOutputReadLine();
        packageUploaderProcess.BeginErrorReadLine();
    }
}
