// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PackageUploader.UI.Model;
using PackageUploader.UI.View;
using PackageUploader.UI.Providers;
using System.Text;
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using System.Windows;

namespace PackageUploader.UI.ViewModel;

public partial class PackageCreationViewModel : BaseViewModel
{
    private const uint CtrlCTerminationCode = 0xc000013a;
    private readonly PackageModelProvider _packageModelService;
    private readonly PathConfigurationProvider _pathConfigurationService;
    private readonly PackageUploader.UI.Utility.IWindowService _windowService;
    private readonly PackingProgressPercentageProvider _packingProgressPercentageProvider;

    private Process? _makePackageProcess;

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            IsErrorVisible = !string.IsNullOrEmpty(value);
        }
    }

    private bool _isErrorVisible;
    public bool IsErrorVisible
    {
        get => _isErrorVisible;
        private set => SetProperty(ref _isErrorVisible, value);
    }

    private string _gameDataPath = string.Empty;
    public string GameDataPath
    {
        get => _gameDataPath;
        set=> SetProperty(ref _gameDataPath, value);
    }

    private string _mappingDataXmlPath = string.Empty;
    public string MappingDataXmlPath
    {
        get => _mappingDataXmlPath;
        set => SetProperty(ref _mappingDataXmlPath, value);
    }

    private bool _isCreationInProgress = false;
    public bool IsCreationInProgress
    {
        get => _isCreationInProgress;
        set => SetProperty(ref _isCreationInProgress, value);
    }

    private bool _isSpinnerRunning = false;
    public bool IsSpinnerRunning
    {
        get => _isSpinnerRunning;
        set => SetProperty(ref _isSpinnerRunning, value);
    }
    
    public double ProgressValue
    {
        get => _packingProgressPercentageProvider.PackingProgressPercentage;
        set
        {
            if (_packingProgressPercentageProvider.PackingProgressPercentage != value)
            {
                _packingProgressPercentageProvider.PackingProgressPercentage = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isProgressVisible = false;
    public bool IsProgressVisible
    {
        get => _isProgressVisible;
        set => SetProperty(ref _isProgressVisible, value);
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
        set => SetProperty(ref _packageFilePath, value);
    }
    private string _gameConfigPath = string.Empty;
    public string GameConfigPath
    {
        get => _gameConfigPath;
        set => SetProperty(ref _gameConfigPath, value);
    }

    private string _subValPath = string.Empty;
    public string SubValPath
    {
        get => _subValPath;
        set => SetProperty(ref _subValPath, value);
    }

    public string PackageSize
    {
        get => Package.PackageSize;
        set
        {
            if (Package.PackageSize != value)
            {
                Package.PackageSize = value;
                OnPropertyChanged();
            }
        }
    }

    private string _bigId = string.Empty;
    public string BigId
    {
        get => _bigId;
        set => SetProperty(ref _bigId, value);
    }

    public string PackageName
    {
        get => Path.GetFileName(Package.PackageFilePath);
    }

    private bool _isMakePkgEnabled = true;
    public bool IsMakePkgEnabled
    {
        get => _isMakePkgEnabled;
        set => SetProperty(ref _isMakePkgEnabled, value);
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

    private string _dragDropMessage = "Drag and drop the Game Data folder here or click to browse";
    public string DragDropMessage
    {
        get => _dragDropMessage;
        set => SetProperty(ref _dragDropMessage, value);
    }

    private bool _encryptEkb = false;
    public bool EncryptEkb
    {
        get => _encryptEkb;
        set => SetProperty(ref _encryptEkb, value);
    }

    public ICommand MakePackageCommand { get; }
    public ICommand GameDataPathDroppedCommand { get; }
    public ICommand BrowseGameDataPathCommand { get; }
    public ICommand ResetGameDataPathCommand { get; }
    public ICommand CancelCreationCommand { get; }
    public ICommand BrowseMappingDataXmlPathCommand { get; }
    public ICommand BrowseGameConfigPathCommand { get; }
    public ICommand BrowsePackageOutputPathCommand { get; }
    public ICommand BrowseSubValPathCommand { get; }
    public ICommand CancelButtonCommand { get; }

    public PackageCreationViewModel(PackageModelProvider packageModelService, 
                                    PathConfigurationProvider pathConfigurationService, 
                                    PackageUploader.UI.Utility.IWindowService windowService, 
                                    PackingProgressPercentageProvider packingProgressPercentageProvider)
    {
        _packageModelService = packageModelService;
        _pathConfigurationService = pathConfigurationService;
        _windowService = windowService;
        _packingProgressPercentageProvider = packingProgressPercentageProvider;

#if WINDOWS
        if (File.Exists(_pathConfigurationService.MakePkgPath))
        {
            IsMakePkgEnabled = true;
        }
        else
        {
            IsMakePkgEnabled = false;
            ErrorMessage = "MakePkg.exe was not found. Please install the GDK in order to package game contents.";
        }
#else
        IsMakePkgEnabled = false;
        ErrorMessage = "MakePkg is not supported on this platform.";
#endif

        MakePackageCommand = new RelayCommand(StartMakePackageProcess);
        GameDataPathDroppedCommand = new RelayCommand<string>(OnGameDataPathDropped);
        BrowseGameDataPathCommand = new RelayCommand(OnBrowseGameDataPath);
        ResetGameDataPathCommand = new RelayCommand(ResetGameDataPath);
        CancelCreationCommand = new RelayCommand(CancelCreation);
        BrowseMappingDataXmlPathCommand = new RelayCommand(OnBrowseMappingDataXml);
        BrowseGameConfigPathCommand = new RelayCommand(OnGameConfigPath);
        BrowsePackageOutputPathCommand = new RelayCommand(OnBrowsePackageOutputPath);
        BrowseSubValPathCommand = new RelayCommand(OnBrowseSubValPath);
        CancelButtonCommand = new RelayCommand(OnCancelButtom);

        GameDataPath = GetPropertyFromApplicationPreferences(nameof(GameDataPath));
        if(GameDataPath != string.Empty)
        {
            IsDragDropVisible = false;
            HasGameDataPath = true;
        }
        MappingDataXmlPath = GetPropertyFromApplicationPreferences(nameof(MappingDataXmlPath));
        GameConfigPath = GetPropertyFromApplicationPreferences(nameof(GameConfigPath));
        LoadGameLogo(GameConfigPath);
    }

#if WINDOWS

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
/*        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _windowService.NavigateTo(typeof(PackageCreationView2));
        });*/
    }
#else
    private void CancelCreation()
    {
        // Non-Windows implementation
    }
#endif

    private async void StartMakePackageProcess()
    { 
        if (IsCreationInProgress)
        {
            return;
        }

        // Reset package data
        _packageModelService.Package = new PackageModel(); //we'll want to capture previous data first
        ErrorMessage = string.Empty;

        if (string.IsNullOrEmpty(GameDataPath))
        {
            ErrorMessage = "Please provide the game data path";
            return;
        }

        ArrayList processOutput = [];
        ArrayList processErrorOutput = [];
        string tempBuildPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string buildPath = String.IsNullOrEmpty(PackageFilePath) ? tempBuildPath : PackageFilePath;
        if(!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        if (string.IsNullOrEmpty(MappingDataXmlPath) || !File.Exists(MappingDataXmlPath))
        {
            // Need to generate our own mapping file
            await GenerateMappingFile(buildPath);
        }

        // Return if we still don't have a mapping file.
        if (string.IsNullOrEmpty(MappingDataXmlPath) || !File.Exists(MappingDataXmlPath))
        {
            return;
        }

        string cmdFormat = "pack /v /f {0} /d {1} /pd {2}";

#if WINDOWS
        SetConsoleCtrlHandler(null, false);
#endif

        _makePackageProcess = new Process();
        _makePackageProcess.StartInfo.FileName = _pathConfigurationService.MakePkgPath;
        _makePackageProcess.StartInfo.Arguments = string.Format(cmdFormat, MappingDataXmlPath, GameDataPath, buildPath);
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
                    // Map the 0-100 range to .05 - .95 range
                    ProgressValue = percentComplete; //percentComplete / 100.0 * 0.9 + 0.05;
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
            IsSpinnerRunning = false;
            string outputString = string.Join("\n", processOutput.ToArray());

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
            File.WriteAllText(logFilePath, outputString);

            if (exitCode != 0)
            {
                if ((uint)exitCode == CtrlCTerminationCode)
                {
                    // User cancelled the process
                    ErrorMessage = "Package creation was cancelled.";
                }
                else
                {
                    // Get the final line from the error message
                    string? errorString = processErrorOutput.Count > 0 ? processErrorOutput[^1]?.ToString() : string.Empty;

                    // Show error message
                    if (!string.IsNullOrEmpty(errorString))
                    {
                        ErrorMessage = $"Error creating package: {errorString}";
                    }
                    else
                    {
                        ErrorMessage = "Error creating package.";
                    }
                }
                return;
            }
            ProgressValue = 1;

            // Navigate using the window service (WPF-specific navigation)
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _windowService.NavigateTo(typeof(PackageUploadView));
            });
        };

        ProgressValue = 0;
        IsSpinnerRunning = true;
        IsProgressVisible = true;
        IsCreationInProgress = true;

        _makePackageProcess.Start();
        _makePackageProcess.BeginOutputReadLine();
        _makePackageProcess.BeginErrorReadLine();

        // Navigate using the window service (WPF-specific navigation)
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _windowService.NavigateTo(typeof(PackagingProgressView));
        });
    }

    private async Task GenerateMappingFile(string tempBuildPath)
    {
        Process? makePackageProcess;
        ArrayList processOutput = [];
        string cmdFormat = "genmap /f {0} /d {1}";

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
                ErrorMessage = "Error generating layout file.";
                return;
            }
            MappingDataXmlPath = layoutFile;
        };

        processOutput.Clear();
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

            string configPath = Path.Combine(GameDataPath, "MicrosoftGame.config");
            if(!File.Exists(configPath))
            {
                IsMakePkgEnabled = false;
                return;
            }
            IsMakePkgEnabled = true;

            GameConfigPath = configPath;
            LoadGameLogo(configPath);

            OnPropertyChanged(nameof(IsDragDropVisible));
        }
    }

    private void OnBrowseMappingDataXml()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
            Title = "Select Mapping Data XML File"
        };

        if (dialog.ShowDialog() == true)
        {
            MappingDataXmlPath = dialog.FileName;
        }
    }
    
    private void OnGameConfigPath()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Config files (*.config)|*.config|All files (*.*)|*.*",
            Title = "Select Game Config File"
        };

        if (dialog.ShowDialog() == true)
        {
            GameConfigPath = dialog.FileName;
            LoadGameLogo(GameConfigPath);
            IsMakePkgEnabled = true;
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

    private void OnCancelButtom()
    {
        // Navigate to the main page view
        _windowService.NavigateTo(typeof(MainPageView));
    }

    private void ResetGameDataPath()
    {
        GameDataPath = string.Empty;
        HasGameDataPath = false;
        OnPropertyChanged(nameof(IsDragDropVisible));
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
        Package.SubValFilePath = SubValPath;
        Package.GameConfigFilePath = GameConfigPath;
    }

    public void OnAppearing()
    {
        if(_packingProgressPercentageProvider.PackingCancelled)
        {
            _packingProgressPercentageProvider.PackingCancelled = false;
            CancelCreation();
        }
    }

    private void LoadGameLogo(string configFilePath)
    {
        if(String.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath))
        {
            return;
        }

        XmlReaderSettings settings = new XmlReaderSettings();

        using(var reader = XmlReader.Create(configFilePath, settings)) 
        {
            while(reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name=="ShellVisuals" && reader.HasAttributes) // HasAttributes should be true
                        {
                            while(reader.MoveToNextAttribute())
                            {
                                if (reader.Name == "StoreLogo")
                                {
                                    var parentDirectory = Directory.GetParent(configFilePath);

                                    if (parentDirectory != null)
                                    {
                                        string path = Path.Combine(parentDirectory.FullName, reader.Value);
                                        if (File.Exists(path))
                                        {
                                            PackagePreviewImage = LoadBitmapImage(path);
                                        }
                                    }
                                }
                            }
                            reader.MoveToElement();
                        }
                        if(reader.Name=="StoreId")
                        {
                            BigId = reader.ReadElementContentAsString();
                        }
                        break;
                }
            }
        }
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
