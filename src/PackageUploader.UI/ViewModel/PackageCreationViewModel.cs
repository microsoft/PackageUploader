// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PackageUploader.UI.Model;
using PackageUploader.UI.View;
using PackageUploader.UI.Providers;
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;
using PackageUploader.UI.Utility;
using Microsoft.Extensions.Logging;

namespace PackageUploader.UI.ViewModel;

public partial class PackageCreationViewModel : BaseViewModel
{
    private const uint CtrlCTerminationCode = 0xc000013a;
    private readonly PackageModelProvider _packageModelService;
    private readonly PathConfigurationProvider _pathConfigurationService;
    private readonly PackingProgressPercentageProvider _packingProgressPercentageProvider;
    private readonly ErrorModelProvider _errorModelProvider;
    private readonly IWindowService _windowService;
    private readonly ILogger<PackageCreationViewModel> _logger;

    private Process? _makePackageProcess;

    private bool _isAdditionalDetailsExpanded = false;
    public bool IsAdditionalDetailsExpanded
    {
        get => _isAdditionalDetailsExpanded;
        set => SetProperty(ref _isAdditionalDetailsExpanded, value);
    }

    private string _gameDataPath = string.Empty;
    public string GameDataPath
    {
        get => _gameDataPath;
        set
        {
            if (_gameDataPath != value)
            {
                _gameDataPath = value;
                OnPropertyChanged(nameof(GameDataPath));
                LoadGameConfigValues();
                EstimatePackageSize();
            }
        }
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
                EstimatePackageSize();

                if (MakePackageCommand is RelayCommand relayCommand)
                {
                    relayCommand.RaiseCanExecuteChanged();
                }
            }
        }
    }

    private bool _isCreationInProgress = false;
    public bool IsCreationInProgress
    {
        get => _isCreationInProgress;
        set => SetProperty(ref _isCreationInProgress, value);
    }

    
    public int ProgressValue
    {
        get => _packingProgressPercentageProvider.PackingProgressPercentage;
        set
        {
            if (_packingProgressPercentageProvider.PackingProgressPercentage != value)
            {
                _packingProgressPercentageProvider.PackingProgressPercentage = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }
    }

    private BitmapImage? _packagePreviewImage = null;
    public BitmapImage? PackagePreviewImage
    {
        get => _packagePreviewImage;
        set => SetProperty(ref _packagePreviewImage, value);
    }

    private BitmapImage? _validatingFilesImage = null;
    public BitmapImage? ValidatingFilesImage
    {
        get => _validatingFilesImage;
        set => SetProperty(ref _validatingFilesImage, value);
    }
    private BitmapImage? _copyingAndEncryptingDataImage = null;
    public BitmapImage? CopyingAndEncryptingDataImage
    {
        get => _copyingAndEncryptingDataImage;
        set => SetProperty(ref _copyingAndEncryptingDataImage, value);
    }
    private BitmapImage? _verifyingPackageContentsImage = null;
    public BitmapImage? VerifyingPackageContentsImage
    {
        get => _verifyingPackageContentsImage;
        set => SetProperty(ref _verifyingPackageContentsImage, value);
    }

    public PackageModel Package => _packageModelService.Package;

    public string _packageFilePath = string.Empty;
    public string PackageFilePath 
    { 
        get => _packageFilePath;
        set
        {
            if (_packageFilePath != value)
            {
                _packageFilePath = value;
                OnPropertyChanged(nameof(PackageFilePath));
            }
        }
    }

    private string _subValPath = string.Empty;
    public string SubValPath
    {
        get => _subValPath;
        set => SetProperty(ref _subValPath, value);
    }

    public string PackageType
    {
        get => Package.PackageType;
        set
        {
            if (Package.PackageType != value)
            {
                Package.PackageType = value;
                OnPropertyChanged();
            }
        }
    }

    public string PackageSize
    {
        get => Package.PackageSize;
        set
        {
            if (Package.PackageSize != value)
            {
                Package.PackageSize = value;
                OnPropertyChanged(nameof(PackageSize));
            }
        }
    }

    private string _gameConfigLoadError = string.Empty;
    public string GameConfigLoadError
    {
        get => _gameConfigLoadError;
        set => SetProperty(ref _gameConfigLoadError, value);
    }

    private string _layoutParseError = string.Empty;
    public string LayoutParseError
    {
        get => _layoutParseError;
        set
        {
            if (_layoutParseError != value)
            {
                SetProperty(ref _layoutParseError, value);
                
                // If an error is being set, expand the details section
                if (!string.IsNullOrEmpty(value))
                {
                    IsAdditionalDetailsExpanded = true;
                }
                
                if (MakePackageCommand is RelayCommand relayCommand)
                {
                    relayCommand.RaiseCanExecuteChanged();
                }
            }
        }
    }

    private string _subValDllError = string.Empty;
    public string SubValDllError
    {
        get => _subValDllError;
        set
        {
            if (_subValDllError != value)
            {
                SetProperty(ref _subValDllError, value);
                
                // If an error is being set, expand the details section
                if (!string.IsNullOrEmpty(value))
                {
                    IsAdditionalDetailsExpanded = true;
                }
            }
        }
    }

    private string _outputDirectoryError = string.Empty;
    public string OutputDirectoryError
    {
        get => _outputDirectoryError;
        set => SetProperty(ref _outputDirectoryError, value);
    }

    private bool _hasValidGameConfig = false;
    public bool HasValidGameConfig
    {
        get => _hasValidGameConfig;
        set
        {
            if (_hasValidGameConfig != value)
            {
                _hasValidGameConfig = value;
                OnPropertyChanged(nameof(HasValidGameConfig));
                if (MakePackageCommand is RelayCommand relayCommand)
                {
                    relayCommand.RaiseCanExecuteChanged();
                }
            }
        }
    }

    private bool _storeIdNotAvailable = false;
    public bool StoreIdNotAvailable
    {
        get => _storeIdNotAvailable;
        set => SetProperty(ref _storeIdNotAvailable, value);
    }

    private string _bigId = string.Empty;
    public string BigId
    {
        get => _bigId;
        set => SetProperty(ref _bigId, value);
    }

    private string _packageId = string.Empty;
    public string PackageId
    {
        get => _packageId;
        set => SetProperty(ref _packageId, value);
    }

    public string PackageName
    {
        get => Path.GetFileName(Package.PackageFilePath);
    }

    private bool _hasGameDataPath;
    public bool HasGameDataPath
    {
        get => _hasGameDataPath;
        set => SetProperty(ref _hasGameDataPath, value);
    }

    private bool _isDragDropVisible = true;
    public bool IsDragDropVisible
    {
        get => _isDragDropVisible && !HasGameDataPath;
        set => SetProperty(ref _isDragDropVisible, value);
    }

    private bool _supportsCustomSubValPath = false;
    public bool SupportsCustomSubValPath
    {
        get => _supportsCustomSubValPath;
        set
        {
            if (_supportsCustomSubValPath != value)
            {
                _supportsCustomSubValPath = value;
                OnPropertyChanged(nameof(SupportsCustomSubValPath));
            }
        }
    }

    public ICommand MakePackageCommand { get; }
    public ICommand GameDataPathDroppedCommand { get; }
    public ICommand BrowseGameDataPathCommand { get; }
    public ICommand BrowseMappingDataXmlPathCommand { get; }
    public ICommand BrowsePackageOutputPathCommand { get; }
    public ICommand BrowseSubValPathCommand { get; }
    public ICommand CancelButtonCommand { get; }

    public PackageCreationViewModel(PackageModelProvider packageModelService,
                                    PathConfigurationProvider pathConfigurationService,
                                    IWindowService windowService,
                                    PackingProgressPercentageProvider packingProgressPercentageProvider,
                                    ILogger<PackageCreationViewModel> logger,
                                    ErrorModelProvider errorModelProvider)
    {
        _packageModelService = packageModelService;
        _pathConfigurationService = pathConfigurationService;
        _windowService = windowService;
        _packingProgressPercentageProvider = packingProgressPercentageProvider;
        _logger = logger;
        _errorModelProvider = errorModelProvider;

        // Ensure our version of MakePkg supports custom SubVal paths before allowing that option.
        var mkgPkgpath = _pathConfigurationService.MakePkgPath;
        var mkgPkgVersionString = FileVersionInfo.GetVersionInfo(mkgPkgpath);

        if (!string.IsNullOrEmpty(mkgPkgVersionString.ProductVersion))
        {
            Version makePkgVersion = new(mkgPkgVersionString.ProductVersion);
            Version firstSupportedVersion = new("10.0.22621.4272"); // June 2023 GDK
            _supportsCustomSubValPath = makePkgVersion >= firstSupportedVersion;

            // Future options can also be checked here to enable new features.
        }

        MakePackageCommand = new RelayCommand(StartMakePackageProcess, CanCreatePackage);
        GameDataPathDroppedCommand = new RelayCommand<string>(OnGameDataPathDropped);
        BrowseGameDataPathCommand = new RelayCommand(OnBrowseGameDataPath);
        BrowseMappingDataXmlPathCommand = new RelayCommand(OnBrowseMappingDataXml);
        BrowsePackageOutputPathCommand = new RelayCommand(OnBrowsePackageOutputPath);
        BrowseSubValPathCommand = new RelayCommand(OnBrowseSubValPath);
        CancelButtonCommand = new RelayCommand(OnCancelButton);

        GameDataPath = GetPropertyFromApplicationPreferences(nameof(GameDataPath));
        if (GameDataPath != string.Empty)
        {
            IsDragDropVisible = false;
            HasGameDataPath = true;
        }
        MappingDataXmlPath = GetPropertyFromApplicationPreferences(nameof(MappingDataXmlPath));

        EstimatePackageSize();
        _errorModelProvider = errorModelProvider;
    }

    private bool CanCreatePackage()
    {
        return HasValidGameConfig && string.IsNullOrEmpty(LayoutParseError) && (string.IsNullOrEmpty(MappingDataXmlPath) || File.Exists(MappingDataXmlPath));
    }

    private void EstimatePackageSize()
    {
        LayoutParseError = string.Empty;
        string sizeInBytes = "Unknown";
        if (string.IsNullOrEmpty(GameDataPath) || !Directory.Exists(GameDataPath))
        {
            PackageSize = "Unknown";
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(MappingDataXmlPath) || !File.Exists(MappingDataXmlPath))
            {
                sizeInBytes = GetDirectorySizeInBytes(GameDataPath).ToString();
            }
            else
            {
                sizeInBytes = ParseLayoutFileForFileSize(MappingDataXmlPath);
            }
    
            double sizeInGB = double.Parse(sizeInBytes) / (1024 * 1024 * 1024); // Convert bytes to GB

            if (sizeInGB < 1)
            {
                PackageSize = "< 1 GB";
            }
            else
            {
                PackageSize = $"{sizeInGB:F2} GB";
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions that may occur during size calculation
            PackageSize = PackageUploader.UI.Resources.Strings.PackageCreation.UnknownText;//"Unknown";
            _logger.LogError(ex, "Error calculating package size.");
        }
    }

    private string ParseLayoutFileForFileSize(string mappingDataXmlPath)
    {
        LayoutParseError = string.Empty;
        try
        {
            _logger.LogInformation("Parsing layout file for size calculation: {Path}", mappingDataXmlPath);

            // Load the XML document
            XmlDocument xmlDoc = new();
            xmlDoc.Load(mappingDataXmlPath);

            // Get all FileGroup nodes
            XmlNodeList? fileGroups = xmlDoc.SelectNodes("//FileGroup");
            if (fileGroups == null || fileGroups.Count == 0)
            {
                _logger.LogWarning("No FileGroup elements found in layout file");
                LayoutParseError = Resources.Strings.PackageCreation.NoFilesInLayoutFileErrorMsg; //"There were no files found in the layout file";
                return "0"; // Return 0 bytes if no file groups found
            }

            long totalSize = 0;
            HashSet<string> processedFiles = new(StringComparer.OrdinalIgnoreCase);

            // Process each FileGroup
            foreach (XmlNode fileGroup in fileGroups)
            {
                string? sourcePath = fileGroup.Attributes?["SourcePath"]?.Value;
                string? include = fileGroup.Attributes?["Include"]?.Value;

                if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(include))
                {
                    continue; // Skip if attributes are missing
                }

                // Resolve path - if relative to game data, use GameDataPath as base
                string fullSourcePath = sourcePath;
                if (!Path.IsPathRooted(sourcePath))
                {
                    fullSourcePath = Path.Combine(GameDataPath, sourcePath);
                }

                // Check if directory exists
                if (!Directory.Exists(fullSourcePath))
                {
                    _logger.LogWarning("Source directory does not exist: {Path}", fullSourcePath);
                    continue;
                }

                // Calculate size based on include pattern
                try
                {
                    SearchOption searchOption = SearchOption.TopDirectoryOnly;

                    // Handle specific file patterns
                    if (include.Contains('*'))
                    {
                        var directory = new DirectoryInfo(fullSourcePath);
                        FileInfo[] files = directory.GetFiles(include, searchOption);

                        foreach (FileInfo file in files)
                        {
                            string normalizedPath = file.FullName.ToLowerInvariant(); // Normalize for comparison
                            if (!processedFiles.Contains(normalizedPath))
                            {
                                totalSize += file.Length;
                                processedFiles.Add(normalizedPath);
                            }
                        }
                    }
                    else
                    {
                        // Handle specific file
                        string filePath = Path.Combine(fullSourcePath, include);
                        string normalizedPath = filePath.ToLowerInvariant(); // Normalize for comparison

                        if (File.Exists(filePath) && !processedFiles.Contains(normalizedPath))
                        {
                            totalSize += new FileInfo(filePath).Length;
                            processedFiles.Add(normalizedPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating size for path: {Path}", fullSourcePath);
                    LayoutParseError = Resources.Strings.PackageCreation.LayoutFileParsingErrorMsg;
                }
            }

            _logger.LogInformation("Finished parsing layout file. Total size: {Size} bytes, Unique files: {FileCount}",
                totalSize, processedFiles.Count);

            return totalSize.ToString();
        }
        catch (XmlException xmlEx)
        {
            _logger.LogError(xmlEx, "XML parsing error in layout file");
            LayoutParseError = Resources.Strings.PackageCreation.LayoutFileParsingErrorMsg; //"There was an error parsing the layout file";
            return GetDirectorySizeInBytes(GameDataPath).ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing layout file for size calculation");
            LayoutParseError = Resources.Strings.PackageCreation.LayoutFileParsingErrorMsg; //"There was an error parsing the layout file";
            return GetDirectorySizeInBytes(GameDataPath).ToString();
        }
    }

    private static long GetDirectorySizeInBytes(string gameDataPath)
    {
        long size = 0;

        DirectoryInfo dirInfo = new(gameDataPath);
        FileInfo[] files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
        foreach (FileInfo file in files)
        {
            size += file.Length;
        }
        return size;
    }

    [DllImport("kernel32.dll")]
    internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool AttachConsole(uint dwProcessId);
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    internal static extern bool FreeConsole();
    [DllImport("kernel32.dll")]
    static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? HandlerRoutine, bool Add);
    // Delegate type to be used as the Handler Routine for SCCH
    delegate Boolean ConsoleCtrlDelegate(uint CtrlType);

    private void CancelCreation()
    {
        uint processId = (uint)(_makePackageProcess?.Id ?? 0);

        if (processId != 0)
        {
            // Getting this to work was a little bit difficult. C# applications
            // don't have a console window so can't use the normal Console.CancelKeyPress
            // event. We can work around this by attaching to the MakePkg console, however
            // that causes the Ctrl+C event to also terminate our application so we
            // need to set the Ctrl Handler to null before sending the event. In order to
            // support subsequent cancellation though, we need to re-enable the Ctrl Handler
            // before creating a new process, or it will inherit the null handler and not
            // be able to cancel. If we do that immediately after sending the Ctrl+C event
            // though, the Ctrl+C event will terminate our application still. So we late
            // bind that and do it right before creating our MakePkg process.
            if (AttachConsole(processId))
            {
                SetConsoleCtrlHandler(null, true);
                GenerateConsoleCtrlEvent(0, 0);
                FreeConsole();
            }
        }
    }

    private async void StartMakePackageProcess()
    { 
        if (IsCreationInProgress)
        {
            return;
        }

        // Reset package data
        _packageModelService.Package = new PackageModel();

        if (string.IsNullOrEmpty(GameDataPath))
        {
            GameConfigLoadError = Resources.Strings.PackageCreation.ProvideGameDataPathErrorMsg; //"Please provide the game data path";
            return;
        }

        OutputDirectoryError = string.Empty;
        LayoutParseError = string.Empty;
        SubValDllError = string.Empty;

        ArrayList processOutput = [];
        ArrayList processErrorOutput = [];
        string tempBuildPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string buildPath = String.IsNullOrEmpty(PackageFilePath) ? tempBuildPath : PackageFilePath;
        if(!Directory.Exists(buildPath))
        {
            try
            {
                Directory.CreateDirectory(buildPath);
            }
            catch(Exception)
            {
                OutputDirectoryError = Resources.Strings.PackageCreation.FailedToCreateOutputDirectoryErrorMsg; //"Failed to create output directory";
                return;
            }
        }

        if (string.IsNullOrEmpty(MappingDataXmlPath) || !File.Exists(MappingDataXmlPath))
        {
            // Need to generate our own mapping file
            await GenerateMappingFile(buildPath);
        }

        // Return if we still don't have a mapping file.
        if (string.IsNullOrEmpty(MappingDataXmlPath) || !File.Exists(MappingDataXmlPath))
        {
            LayoutParseError = Resources.Strings.PackageCreation.FailedToGenerateLayoutFileErrorMsg; //"Failed to generate a layout file";
            return;
        }

        string cmdFormat = "pack /v /f \"{0}\" /d \"{1}\" /pd \"{2}\"";
        string arguments = string.Format(cmdFormat, MappingDataXmlPath, GameDataPath, buildPath);

        SubValDllError = string.Empty;
        if (!string.IsNullOrEmpty(SubValPath))
        {
            if (File.Exists(Path.Combine(SubValPath, "SubmissionValidator.dll")))
            {
                arguments += $" /validationpath \"{SubValPath}\"";
            }
            else
            {
                SubValDllError = Resources.Strings.PackageCreation.SubValDllNotFoundErrorMsg; //"SubmissionValidator.dll not found in the specified path.";
                return;
            }
        }

        SetConsoleCtrlHandler(null, false);

        _makePackageProcess = new Process();
        _makePackageProcess.StartInfo.FileName = _pathConfigurationService.MakePkgPath;
        _makePackageProcess.StartInfo.Arguments = arguments;
        _makePackageProcess.StartInfo.RedirectStandardOutput = true;
        _makePackageProcess.StartInfo.RedirectStandardError = true;
        _makePackageProcess.EnableRaisingEvents = true;
        _makePackageProcess.StartInfo.CreateNoWindow = true;
        ProgressValue = 0;

        _makePackageProcess.OutputDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);

                // Check for encryption progress messages
                var match = EncryptionProgressRegex().Match(args.Data);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int percentComplete))
                {
                    // Map the 0-100 range to 5-95 to allow for setup and validation
                    ProgressValue = (int)(percentComplete * 0.9 + 5);
                }
            }
        };
        
        _makePackageProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processErrorOutput.Add(args.Data);
            }
        };
        
        _makePackageProcess.Exited += (sender, args) =>
        {
            string outputString = string.Join("\n", processOutput.ToArray());

            // Log error output as well
            outputString += "\n" + string.Join("\n", processErrorOutput.ToArray());

            // Parse Make Package Output
            ProcessMakePackageOutput(outputString);

            if (!_makePackageProcess.HasExited)
            {
                _makePackageProcess.WaitForExit();
            }
            int exitCode = _makePackageProcess.ExitCode;

            IsCreationInProgress = false;

            // Log the output to a file for debugging
            string logFilePath = Path.Combine(Path.GetTempPath(), $"PackageUploader_UI_MakePkg_{DateTime.Now:yyyyMMddHHmmss}.log");
            _packageModelService.PackagingLogFilepath = logFilePath;

            File.WriteAllText(logFilePath, outputString);

            if (exitCode != 0)
            {
                if ((uint)exitCode == CtrlCTerminationCode)
                {
                    // User cancelled the process, progress screen
                    // will already navigate back to this screen so
                    // no work is needed.
                }
                else
                {
                    // Get the stderr output for our error message
                    string? errorString = string.Join("\n", processErrorOutput.ToArray());

                    // Add error message so progress screen can display it.
                    if (!string.IsNullOrEmpty(errorString))
                    {
                        _errorModelProvider.Error.MainMessage = PackageUploader.UI.Resources.Strings.PackageCreation.ErrorCreatingPackageErrorMsg; //"Error creating package.";
                        _errorModelProvider.Error.DetailMessage = errorString;
                        _errorModelProvider.Error.OriginPage = typeof(PackageCreationView);
                    }
                    else
                    {
                        _errorModelProvider.Error.MainMessage = PackageUploader.UI.Resources.Strings.PackageCreation.ErrorCreatingPackageErrorMsg; //"Error creating package.";
                        _errorModelProvider.Error.OriginPage = typeof(PackageCreationView);
                    }

                    _errorModelProvider.Error.LogsPath = logFilePath;
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        _windowService.NavigateTo(typeof(ErrorPageView));
                    });

                    _logger.LogError("Package creation failed with exit code {ExitCode}.", exitCode);
                }
                return;
            }
            ProgressValue = 100;

            // Navigate using the window service
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.NavigateTo(typeof(PackagingFinishedView));
            });
        };

        ProgressValue = 0;
        IsCreationInProgress = true;

        _logger.LogInformation("Calling '{Command}'", _makePackageProcess.StartInfo.FileName + " " + _makePackageProcess.StartInfo.Arguments);
        _makePackageProcess.Start();
        _makePackageProcess.BeginOutputReadLine();
        _makePackageProcess.BeginErrorReadLine();

        // Navigate using the window service
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _windowService.NavigateTo(typeof(PackagingProgressView));
        });
    }

    private async Task GenerateMappingFile(string tempBuildPath)
    {
        Process? makePackageProcess;
        ArrayList processOutput = [];
        string cmdFormat = "genmap /f \"{0}\" /d \"{1}\"";

        string layoutFile = Path.Combine(tempBuildPath, "generated_layout.xml");

        makePackageProcess = new Process();
        makePackageProcess.StartInfo.FileName = _pathConfigurationService.MakePkgPath;
        makePackageProcess.StartInfo.Arguments = string.Format(cmdFormat, layoutFile, GameDataPath);
        makePackageProcess.StartInfo.RedirectStandardOutput = true;
        makePackageProcess.StartInfo.RedirectStandardError = true;
        makePackageProcess.EnableRaisingEvents = true;
        makePackageProcess.StartInfo.CreateNoWindow = true;

        makePackageProcess.OutputDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };

        makePackageProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!String.IsNullOrEmpty(args.Data))
            {
                processOutput.Add(args.Data);
            }
        };

        makePackageProcess.Exited += (sender, args) =>
        {
            string outputString = string.Join("\n", processOutput.ToArray());
            makePackageProcess.WaitForExit();

            // Log the output to a file for debugging
            string logFilePath = Path.Combine(Path.GetTempPath(), $"PackageUploader_UI_GenMap_{DateTime.Now:yyyyMMddHHmmss}.log");
            File.WriteAllText(logFilePath, outputString);

            if (makePackageProcess.ExitCode != 0)
            {
                // Show error message
                LayoutParseError = Resources.Strings.PackageCreation.GeneratingLayoutFileErrorMsg; //"Error generating layout file.";

                _logger.LogError("Error generating layout file. Exit code: {ExitCode}", makePackageProcess.ExitCode);
                return;
            }
            MappingDataXmlPath = layoutFile;
        };

        processOutput.Clear();

        _logger.LogInformation("Calling '{Command}'", makePackageProcess.StartInfo.FileName + " " + makePackageProcess.StartInfo.Arguments);
        makePackageProcess.Start();
        makePackageProcess.BeginOutputReadLine();
        makePackageProcess.BeginErrorReadLine();

        await makePackageProcess.WaitForExitAsync();
    }

    private void OnGameDataPathDropped(string path)
    {
        GameDataPath = path;
        HasGameDataPath = true;
        OnPropertyChanged(nameof(IsDragDropVisible));
    }

    private void OnBrowseGameDataPath()
    {
        var folderDialog = new FolderBrowserDialog();
        if (folderDialog.ShowDialog() == DialogResult.OK)
        {
            GameDataPath = folderDialog.SelectedPath;
            HasGameDataPath = true;

            OnPropertyChanged(nameof(IsDragDropVisible));
        }
    }

    private void OnBrowseMappingDataXml()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
            Title = PackageUploader.UI.Resources.Strings.PackageCreation.SelectMapXmlTitleText //"Select Mapping Data XML File"
        };

        if (dialog.ShowDialog() == true)
        {
            MappingDataXmlPath = dialog.FileName;
        }
    }

    private void OnBrowsePackageOutputPath()
    {
        var folderDialog = new FolderBrowserDialog();
        if (folderDialog.ShowDialog() == DialogResult.OK)
        {
            PackageFilePath = folderDialog.SelectedPath;
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

    private void OnCancelButton()
    {
        // Navigate to the main page view
        _windowService.NavigateTo(typeof(MainPageView));
    }

    [GeneratedRegex(@"Successfully created package '(?<PackagePath>.*?\.xvc)'")]
    private static partial Regex XvcPackagePathRegex();

    [GeneratedRegex(@"Successfully created package '(?<PackagePath>.*?\.msixvc)'")]
    private static partial Regex MsixvcPackagePathRegex();

    [GeneratedRegex(@"Encrypted (\d+) %")]
    private static partial Regex EncryptionProgressRegex();

    private void ProcessMakePackageOutput(string outputString)
    {
        // Package Path for XVC
        MatchCollection packagePathMatchCollection = XvcPackagePathRegex().Matches(outputString);
        
        for (int i = 0; i < packagePathMatchCollection.Count; i++)
        {
            string packagePathValue = packagePathMatchCollection[i].Groups["PackagePath"].Value;
            if (packagePathValue != null)
            {
                Package.PackageFilePath = packagePathValue;
                break;
            }
        }

        // Package Path for MSIXVC
        MatchCollection msixvcPackagePathMatchCollection = MsixvcPackagePathRegex().Matches(outputString);

        for (int i = 0; i < msixvcPackagePathMatchCollection.Count; i++)
        {
            string packagePathValue = msixvcPackagePathMatchCollection[i].Groups["PackagePath"].Value;
            if (packagePathValue != null)
            {
                Package.PackageFilePath = packagePathValue;
                break;
            }
        }
        Package.GameConfigFilePath = Path.Combine(GameDataPath, "MicrosoftGame.config");
    }

    public void OnAppearing()
    {
        if(_packingProgressPercentageProvider.PackingCancelled)
        {
            _packingProgressPercentageProvider.PackingCancelled = false;
            CancelCreation();
        }
    }

    private void LoadGameConfigValues()
    {
        GameConfigLoadError = string.Empty;
        HasValidGameConfig = false;

        if (string.IsNullOrEmpty(GameDataPath) || !Directory.Exists(GameDataPath))
        {           
            return;
        }

        string gameConfigPath = Path.Combine(GameDataPath, "MicrosoftGame.config");
        if (String.IsNullOrEmpty(gameConfigPath) || !File.Exists(gameConfigPath))
        {
            GameConfigLoadError = Resources.Strings.PackageCreation.FolderDoesNotContainConfigErrMsg;  //"Provided folder does not contain a MicrosoftGame.config file";
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
            GameConfigLoadError = Resources.Strings.PackageCreation.MicrosoftGameConfigInvalidErrMsg; //"The MicrosoftGame.config in the provided folder is invalid";
            return;
        }

        if (string.IsNullOrEmpty(gameConfig.Identity.Name))
        {
            GameConfigLoadError = Resources.Strings.PackageCreation.MsftGameCOnfigLacksValidIdentityErrMsg;//"The MicrosoftGame.config lacks a valid Identity node";
            return;
        }
        if (string.IsNullOrEmpty(gameConfig.ShellVisuals.Square150x150Logo))
        {
            GameConfigLoadError = Resources.Strings.PackageCreation.MsftGameConfigMissingLogoFilesErrMsg;//"The MicrosoftGame.config does not have the required Logo files in the ShellVisuals";
            return;
        }
        if (!File.Exists(gameConfig.ShellVisuals.Square150x150Logo))
        {
            GameConfigLoadError = Resources.Strings.PackageCreation.MsftGameConfigLogoFileNotExistErrMsg;//"The logo file specified by the MicrosoftGame.config does not exist";
            return;
        }

        if (string.IsNullOrEmpty(gameConfig.StoreId))
        {
            StoreIdNotAvailable = true;
            BigId = "None";
        }
        else
        {
            StoreIdNotAvailable = false;
            BigId = gameConfig.StoreId;
        }
        
        PackageId = gameConfig.Identity.Name;
        PackagePreviewImage = LoadBitmapImage(gameConfig.ShellVisuals.Square150x150Logo);

        try
        {
            PackageType = gameConfig.GetDeviceFamily();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device family from game config.");
            GameConfigLoadError = ex.Message;
            return;
        }

        HasValidGameConfig = true;
    }

    private static BitmapImage LoadBitmapImage(string imagePath)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(imagePath);
        image.EndInit();
        return image;
    }
}
